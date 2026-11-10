using Azure;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeNChillFunctions.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UsersTableService _usersService;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<object> _passwordHasher = new();

        public AuthController(UsersTableService usersService, IConfiguration configuration)
        {
            _usersService = usersService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Role) || string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest("Email, Password, Role, and FullName are required."); // Plain-text error[cite: 1]
            }

            string role = dto.Role.Trim(); // Accepts any role name (Admin, Staff, Banana)[cite: 1, 2]
            string email = dto.Email.Trim().ToLowerInvariant();

            var existingUser = await _usersService.GetUserAsync(role, email);
            if (existingUser != null)
            {
                return BadRequest("An account with this email already exists for the specified role."); // Plain-text error[cite: 1]
            }

            var userEntity = new UserEntity
            {
                PartitionKey = role,
                RowKey = email,
                FullName = dto.FullName.Trim(),
                PasswordHash = _passwordHasher.HashPassword(new object(), dto.Password),
                CreatedDate = DateTimeOffset.UtcNow
            };

            await _usersService.AddUserAsync(userEntity);

            return Ok(new
            {
                message = "User registered successfully.",
                email = email,
                role = role
            }); // JSON success[cite: 1]
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest("Email, Password, and Role are required."); // Plain-text error[cite: 1]
            }

            string role = dto.Role.Trim();
            string email = dto.Email.Trim().ToLowerInvariant();

            var user = await _usersService.GetUserAsync(role, email);
            if (user == null)
            {
                return Unauthorized("User not found under the specified role."); // Plain-text error[cite: 1]
            }

            var verification = _passwordHasher.VerifyHashedPassword(new object(), user.PasswordHash, dto.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials."); // Plain-text error[cite: 1]
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                email = user.RowKey,
                role = user.PartitionKey,
                fullName = user.FullName
            }); // JSON success[cite: 1]
        }

        private string GenerateJwtToken(UserEntity user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"] ?? "CoffeeNChillSuperSecretKey2026!@#SecureToken";
            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.RowKey),
                new Claim(ClaimTypes.Role, user.PartitionKey) // Role claim for [Authorize(Roles = "...")]
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = jwtSettings["Issuer"] ?? "CoffeeNChillGateway",
                Audience = jwtSettings["Audience"] ?? "CoffeeNChillUsers",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}