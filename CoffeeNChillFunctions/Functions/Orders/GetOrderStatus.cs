using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class GetOrderStatus
    {
        private readonly OrderTableService _orderTableService;

        public GetOrderStatus(OrderTableService orderTableService)
        {
            _orderTableService = orderTableService;
        }

        [Function("GetOrderStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "orders/{orderId}")]
            HttpRequestData request,
            string orderId,
            FunctionContext context)
        {
            OrderEntity? order =
                await _orderTableService.GetOrderAsync(
                    orderId,
                    context.CancellationToken);

            if (order == null)
            {
                HttpResponseData notFound =
                    request.CreateResponse(HttpStatusCode.NotFound);

                await notFound.WriteAsJsonAsync(new
                {
                    error = $"Order '{orderId}' was not found."
                });

                return notFound;
            }

            List<string> selectedItemSkus =
                JsonSerializer.Deserialize<List<string>>(
                    order.SelectedItemSkusJson) ?? new();

            HttpResponseData response =
                request.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                orderId = order.RowKey,
                customerName = order.CustomerName,
                selectedItemSkus,
                totalPrice = order.TotalPrice,
                status = order.Status,
                orderTimestamp = order.OrderTimestamp,
                lastUpdated = order.LastUpdated
            });

            return response;
        }
    }
}