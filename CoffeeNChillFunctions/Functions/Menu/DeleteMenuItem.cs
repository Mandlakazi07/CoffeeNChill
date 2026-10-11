using Azure;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Menu
{
    public class DeleteMenuItem
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<DeleteMenuItem> _logger;

        public DeleteMenuItem(
            MenuTableService menuTableService,
            ILogger<DeleteMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("DeleteMenuItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "delete",
                Route = "menu/{category}/{id}")] HttpRequest request,
            string category,
            string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) ||
                    string.IsNullOrWhiteSpace(id))
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "Category and ID are required."
                    });
                }

                string cleanedCategory = category.Trim();
                string cleanedId = id.Trim();

                var existingItem =
                    await _menuTableService.GetMenuItemAsync(
                        cleanedCategory,
                        cleanedId);

                if (existingItem == null)
                {
                    return new NotFoundObjectResult(new
                    {
                        message = "Menu item not found."
                    });
                }

                await _menuTableService.DeleteMenuItemAsync(
                    cleanedCategory,
                    cleanedId);

                _logger.LogInformation(
                    "Menu item {MenuItemId} in category {Category} was deleted.",
                    cleanedId,
                    cleanedCategory);

                return new OkObjectResult(new
                {
                    message = "Menu item deleted successfully.",
                    category = cleanedCategory,
                    id = cleanedId
                });
            }
            catch (RequestFailedException exception)
                when (exception.Status == 404)
            {
                _logger.LogWarning(
                    exception,
                    "Menu item {MenuItemId} was not found during deletion.",
                    id);

                return new NotFoundObjectResult(new
                {
                    message = "Menu item not found."
                });
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(
                    exception,
                    "Azure Table Storage failed while deleting menu item {MenuItemId}.",
                    id);

                return new ObjectResult(new
                {
                    message =
                        "The menu item could not be deleted because storage is unavailable."
                })
                {
                    StatusCode =
                        StatusCodes.Status503ServiceUnavailable
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while deleting menu item {MenuItemId}.",
                    id);

                return new ObjectResult(new
                {
                    message =
                        "An unexpected error occurred while deleting the menu item."
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}