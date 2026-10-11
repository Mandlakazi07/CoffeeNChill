using System.Text.Json;
using Azure;
using CoffeeNChillFunctions.DTOs;
using CoffeeNChillFunctions.Services;
using CoffeeNChillFunctions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MenuItemEntity = CoffeeNChillFunctions.Models.MenuItem;

namespace CoffeeNChillFunctions.Functions.Menu
{
    public class CreateMenuItem
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<CreateMenuItem> _logger;

        public CreateMenuItem(
            MenuTableService menuTableService,
            ILogger<CreateMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("CreateMenuItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "menu")] HttpRequest request)
        {
            try
            {
                CreateMenuItemDto? input =
                    await JsonSerializer.DeserializeAsync<CreateMenuItemDto>(
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
                    MenuItemValidator.ValidateCreate(input);

                if (validationErrors.Count > 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "Validation failed.",
                        errors = validationErrors
                    });
                }

                MenuItemEntity menuItem = new()
                {
                    PartitionKey = input.Category.Trim(),
                    RowKey = input.Id.Trim(),
                    Name = input.Name.Trim(),
                    Description =
                        input.Description?.Trim() ?? string.Empty,
                    Price = input.Price,
                    IsAvailable = input.IsAvailable
                };

                await _menuTableService.AddMenuItemAsync(menuItem);

                _logger.LogInformation(
                    "Menu item {MenuItemId} was created in category {Category}.",
                    menuItem.RowKey,
                    menuItem.PartitionKey);

                return new ObjectResult(new
                {
                    message = "Menu item created successfully.",
                    item = menuItem
                })
                {
                    StatusCode = StatusCodes.Status201Created
                };
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Invalid JSON was provided when creating a menu item.");

                return new BadRequestObjectResult(new
                {
                    message = "The JSON request body is invalid."
                });
            }
            catch (RequestFailedException exception)
                when (exception.Status == 409)
            {
                _logger.LogWarning(
                    "A duplicate menu item was submitted.");

                return new ConflictObjectResult(new
                {
                    message =
                        "A menu item with this category and ID already exists."
                });
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(
                    exception,
                    "Azure Table Storage failed while creating a menu item.");

                return new ObjectResult(new
                {
                    message = "The menu item could not be stored."
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
                    "An unexpected error occurred while creating a menu item.");

                return new ObjectResult(new
                {
                    message = "An unexpected error occurred."
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}