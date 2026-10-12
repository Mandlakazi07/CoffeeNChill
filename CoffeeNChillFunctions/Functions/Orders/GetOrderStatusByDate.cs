/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * These date-based routes implement the CoffeeNChill assessment contract using the project's Orders table service; Azure.Data.Tables API behaviour is documented by Microsoft (Microsoft, n.d.-d).
 * See docs/CODE_ATTRIBUTION.md for project lineage, technical references and AI-use disclosure (EMKNDN, 2026; OpenAI, 2026).
 */

using System.Globalization;
using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class GetOrderStatusByDate
    {
        private readonly OrderTableService _orderTableService;

        public GetOrderStatusByDate(OrderTableService orderTableService)
        {
            _orderTableService = orderTableService;
        }

        [Function("GetOrderStatusByDate")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "orders/{orderDate}/{orderId}")]
            HttpRequestData request,
            string orderDate,
            string orderId,
            FunctionContext context)
        {
            if (!DateTime.TryParseExact(
                    orderDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                HttpResponseData badRequest =
                    request.CreateResponse(HttpStatusCode.BadRequest);

                await badRequest.WriteAsJsonAsync(new
                {
                    error = "Date must be in yyyy-MM-dd format."
                });

                return badRequest;
            }

            OrderEntity? order = await _orderTableService.GetOrderAsync(
                orderDate,
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
                orderDate = order.PartitionKey,
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
