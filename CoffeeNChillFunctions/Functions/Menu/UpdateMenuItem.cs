using System.Text.Json;
using Azure;
using CoffeeNChillFunctions.DTOs;
using CoffeeNChillFunctions.Services;
using CoffeeNChillFunctions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Menu
{
    public class UpdateMenuItem
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<UpdateMenuItem> _logger;

        public UpdateMenuItem(
            MenuTableService menuTableService,
            ILogger<UpdateMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("UpdateMenuItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
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

                UpdateMenuItemDto? input =
                    await JsonSerializer.DeserializeAsync<UpdateMenuItemDto>(
                        request.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (input == null)
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "A request body is required."
                    });
                }

                List<string> validationErrors =
                    MenuItemValidator.ValidateUpdate(input);

                if (validationErrors.Count > 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "Validation failed.",
                        errors = validationErrors
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

                if (input.Price.HasValue)
                {
                    existingItem.Price = input.Price.Value;
                }

                if (input.IsAvailable.HasValue)
                {
                    existingItem.IsAvailable =
                        input.IsAvailable.Value;
                }

                await _menuTableService
                    .UpdateMenuItemAsync(existingItem);

                _logger.LogInformation(
                    "Menu item {MenuItemId} in category {Category} was updated.",
                    existingItem.RowKey,
                    existingItem.PartitionKey);

                return new OkObjectResult(new
                {
                    message = "Menu item updated successfully.",
                    item = existingItem
                });
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Invalid JSON was provided while updating menu item {Id}.",
                    id);

                return new BadRequestObjectResult(new
                {
                    message = "The JSON request body is invalid."
                });
            }
            catch (RequestFailedException exception)
                when (exception.Status == 404)
            {
                _logger.LogWarning(
                    exception,
                    "Menu item {Id} was not found.",
                    id);

                return new NotFoundObjectResult(new
                {
                    message = "Menu item not found."
                });
            }
            catch (RequestFailedException exception)
                when (exception.Status == 412)
            {
                _logger.LogWarning(
                    exception,
                    "A concurrency conflict occurred while updating menu item {Id}.",
                    id);

                return new ConflictObjectResult(new
                {
                    message =
                        "The menu item was changed by another request. Refresh it and try again."
                });
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(
                    exception,
                    "Azure Table Storage failed while updating menu item {Id}.",
                    id);

                return new ObjectResult(new
                {
                    message = "The menu item could not be updated."
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
                    "An unexpected error occurred while updating menu item {Id}.",
                    id);

                return new ObjectResult(new
                {
                    message =
                        "An unexpected error occurred while updating the menu item."
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}