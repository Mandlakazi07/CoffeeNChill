/* 
 * Code Attribution:
 * Title: JWT Authentication and Azure Table Storage
 * Author: Microsoft Learn / Stack Overflow (BCrypt implementation)
 * Date: 30 October 2026
 * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Azure.Data.Tables;
using CoffeeNChillGateway.Models;

// Handles Login and Registration with BCrypt and JWT`nnamespace CoffeeNChillGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TableClient _tableClient;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration.GetConnectionString("AzureStorage") ?? _configuration["AzureStorage:ConnectionString"];
            _tableClient = new TableClient(connectionString, "Users");
            _tableClient.CreateIfNotExists();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email and password are required.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new UserProfile
            {
                PartitionKey = request.Role,
                RowKey = request.Email.ToLower(),
                PasswordHash = passwordHash,
                FullName = request.FullName,
                CreatedDate = DateTimeOffset.UtcNow
            };

            try
            {
                await _tableClient.AddEntityAsync(user);
                return Ok(new { message = "User registered successfully." });
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 409)
            {
                return Conflict("User already exists.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var users = _tableClient.QueryAsync<UserProfile>(u => u.RowKey == request.Email.ToLower());
                var user = await users.FirstOrDefaultAsync();

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return Unauthorized("Invalid email or password.");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.RowKey),
                        new Claim(ClaimTypes.Role, user.PartitionKey)
                    }),
                    Expires = DateTime.UtcNow.AddHours(2),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(new { token = tokenHandler.WriteToken(token) });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred during login.");
            }
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
