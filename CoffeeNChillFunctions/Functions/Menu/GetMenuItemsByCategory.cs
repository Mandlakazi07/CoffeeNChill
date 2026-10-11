using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Menu
{
    public class GetMenuItemsByCategory
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<GetMenuItemsByCategory> _logger;

        public GetMenuItemsByCategory(
            MenuTableService menuTableService,
            ILogger<GetMenuItemsByCategory> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu/category/{category}")] HttpRequest request,
            string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "A category is required."
                    });
                }

                string cleanedCategory = category.Trim();

                var menuItems = await _menuTableService
                    .GetMenuItemsByCategoryAsync(cleanedCategory);

                return new OkObjectResult(new
                {
                    category = cleanedCategory,
                    count = menuItems.Count,
                    items = menuItems
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to retrieve items in category {Category}.",
                    category);

                return new ObjectResult(new
                {
                    message = "An error occurred while retrieving menu items."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
