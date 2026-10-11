using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class UpdateOrderStatus
    {
        private readonly OrderTableService _orderTableService;
        private readonly ILogger<UpdateOrderStatus> _logger;

        public UpdateOrderStatus(
            OrderTableService orderTableService,
            ILogger<UpdateOrderStatus> logger)
        {
            _orderTableService = orderTableService;
            _logger = logger;
        }

        [Function("UpdateOrderStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
                Route = "orders/{orderId}/status")]
            HttpRequestData request,
            string orderId,
            FunctionContext context)
        {
            try
            {
                JsonDocument body =
                    await JsonDocument.ParseAsync(
                        request.Body,
                        cancellationToken:
                            context.CancellationToken);

                if (!body.RootElement.TryGetProperty(
                        "status",
                        out JsonElement statusElement))
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.BadRequest,
                        "The status field is required.");
                }

                string newStatus =
                    statusElement.GetString()?.Trim()
                    ?? string.Empty;

                string[] allowedStatuses =
                {
                    OrderStatus.Received,
                    OrderStatus.Preparing,
                    OrderStatus.Ready,
                    OrderStatus.Collected
                };

                string? validStatus =
                    allowedStatuses.FirstOrDefault(status =>
                        status.Equals(
                            newStatus,
                            StringComparison.OrdinalIgnoreCase));

                if (validStatus == null)
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.BadRequest,
                        "Status must be Received, Preparing, Ready or Collected.");
                }

                OrderEntity? existingOrder =
                    await _orderTableService.GetOrderAsync(
                        orderId,
                        context.CancellationToken);

                if (existingOrder == null)
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.NotFound,
                        $"Order '{orderId}' was not found.");
                }

                await _orderTableService.UpdateStatusAsync(
                    orderId,
                    validStatus,
                    context.CancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} updated to {Status}.",
                    orderId,
                    validStatus);

                HttpResponseData response =
                    request.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    orderId,
                    status = validStatus,
                    message = "Order status updated successfully."
                });

                return response;
            }
            catch (JsonException)
            {
                return await CreateErrorResponse(
                    request,
                    HttpStatusCode.BadRequest,
                    "The request body contains invalid JSON.");
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