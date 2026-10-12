using System.Net;
using System.Globalization;
using System.Text.Json;
using CoffeeNChillFunctions.DTOs;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using CoffeeNChillFunctions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class QueueOrder
    {
        private readonly OrderQueueService _queueService;
        private readonly ILogger<QueueOrder> _logger;

        public QueueOrder(
            OrderQueueService queueService,
            ILogger<QueueOrder> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        [Function("QueueOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "orders/queue")]
            HttpRequestData request,
            FunctionContext context)
        {
            try
            {
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true
                };

                OrderRequest? orderRequest =
                    await JsonSerializer.DeserializeAsync<OrderRequest>(
                        request.Body,
                        options,
                        context.CancellationToken);

                if (orderRequest == null)
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.BadRequest,
                        "A valid order request is required.");
                }

                List<string> validationErrors =
                    OrderRequestValidator.Validate(orderRequest);

                if (validationErrors.Count > 0)
                {
                    HttpResponseData validationResponse =
                        request.CreateResponse(HttpStatusCode.BadRequest);

                    await validationResponse.WriteAsJsonAsync(new
                    {
                        message = "Order validation failed.",
                        errors = validationErrors
                    });

                    return validationResponse;
                }

                DateTimeOffset orderTimestamp =
                    orderRequest.OrderTimestamp ?? DateTimeOffset.UtcNow;

                OrderQueueMessage queueMessage = new()
                {
                    OrderId = orderRequest.OrderId.Trim(),
                    CustomerName = orderRequest.CustomerName.Trim(),
                    SelectedItemSkus = orderRequest.SelectedItemSkus,
                    TotalPrice = orderRequest.TotalPrice,
                    OrderTimestamp = orderTimestamp
                };

                await _queueService.QueueOrderAsync(
                    queueMessage,
                    context.CancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} was added to the processing queue.",
                    queueMessage.OrderId);

                string orderDate = queueMessage.OrderTimestamp.UtcDateTime
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                QueueOrderResponse responseBody = new()
                {
                    OrderId = queueMessage.OrderId,
                    Status = OrderStatus.Queued,
                    OrderDate = orderDate,
                    StatusUrl = $"/api/orders/{orderDate}/{Uri.EscapeDataString(queueMessage.OrderId)}"
                };

                HttpResponseData response =
                    request.CreateResponse(HttpStatusCode.Accepted);

                await response.WriteAsJsonAsync(responseBody);

                return response;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Invalid JSON was submitted to the order endpoint.");

                return await CreateErrorResponse(
                    request,
                    HttpStatusCode.BadRequest,
                    "The request body contains invalid JSON.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while queuing an order.");

                return await CreateErrorResponse(
                    request,
                    HttpStatusCode.InternalServerError,
                    "The order could not be queued.");
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponse(
            HttpRequestData request,
            HttpStatusCode statusCode,
            string message)
        {
            HttpResponseData response =
                request.CreateResponse(statusCode);

            await response.WriteAsJsonAsync(new
            {
                error = message
            });

            return response;
        }
    }
}