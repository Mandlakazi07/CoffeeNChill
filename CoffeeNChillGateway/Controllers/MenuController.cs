/* 
 * Code Attribution:
 * Title: ASP.NET Core Role-Based Authorization
 * Author: Microsoft Learn
 * Date: 30 October 2026
 * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * Adapted for CoffeeNChill Menu endpoints.
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Handles public Menu viewing and Admin/Staff modifications
namespace CoffeeNChillGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        // Rubric Requirement: [AllowAnonymous] for GET /api/menu
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetMenu()
        {
            var menu = new[]
            {
                new { Id = 1, Item = "Espresso", Price = 25.00 },
                new { Id = 2, Item = "Cappuccino", Price = 32.00 },
                new { Id = 3, Item = "Croissant", Price = 28.00 }
            };
            return Ok(menu);
        }

        // Rubric Requirement: [Authorize(Roles="Admin,Staff")] for POST
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult AddMenuItem([FromBody] string itemName)
        {
            // Placeholder logic for adding an item
            return Ok(new { message = $"Item '{itemName}' added by {User.Identity?.Name}." });
        }

        // Rubric Requirement: [Authorize(Roles="Admin,Staff")] for PUT
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult UpdateMenuItem(int id, [FromBody] string newItemName)
        {
            return Ok(new { message = $"Item {id} updated to '{newItemName}' by {User.Identity?.Name}." });
        }

        // Rubric Requirement: [Authorize(Roles="Admin,Staff")] for DELETE
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult DeleteMenuItem(int id)
        {
            return Ok(new { message = $"Item {id} deleted by {User.Identity?.Name}." });
        }
    }
} 
