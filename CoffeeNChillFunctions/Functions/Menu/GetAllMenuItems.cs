using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Menu
{
    public class GetAllMenuItems
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<GetAllMenuItems> _logger;

        public GetAllMenuItems(
            MenuTableService menuTableService,
            ILogger<GetAllMenuItems> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("GetAllMenuItems")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu")] HttpRequest request)
        {
            try
            {
                var menuItems =
                    await _menuTableService.GetAllMenuItemsAsync();

                return new OkObjectResult(new
                {
                    count = menuItems.Count,
                    items = menuItems
                });

            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while retrieving menu items.");

                return new ObjectResult(new
                {
                    message = "An unexpected error occurred while retrieving menu items."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}