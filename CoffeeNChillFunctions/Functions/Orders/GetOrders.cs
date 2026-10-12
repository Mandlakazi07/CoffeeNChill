/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * These date-based routes implement the CoffeeNChill assessment contract using the project's Orders table service; Azure.Data.Tables API behaviour is documented by Microsoft (Microsoft, n.d.-d).
 * See docs/CODE_ATTRIBUTION.md for project lineage, technical references and AI-use disclosure (EMKNDN, 2026; OpenAI, 2026).
 */

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Web;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class GetOrders
    {
        private readonly OrderTableService _orderTableService;

        public GetOrders(OrderTableService orderTableService)
        {
            _orderTableService = orderTableService;
        }

        [Function("GetOrders")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "orders")]
            HttpRequestData request,
            FunctionContext context)
        {
            string? date = HttpUtility.ParseQueryString(request.Url.Query).Get("date");

            if (date != null && !DateTime.TryParseExact(
                    date,
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

            List<OrderEntity> orders = date == null
                ? await _orderTableService.GetAllOrdersAsync(context.CancellationToken)
                : await _orderTableService.GetOrdersByDateAsync(date, context.CancellationToken);

            HttpResponseData response =
                request.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                date,
                count = orders.Count,
                orders = orders.Select(order => new
                {
                    orderDate = order.PartitionKey,
                    orderId = order.RowKey,
                    customerName = order.CustomerName,
                    selectedItemSkus = JsonSerializer.Deserialize<List<string>>(
                        order.SelectedItemSkusJson) ?? new List<string>(),
                    totalPrice = order.TotalPrice,
                    status = order.Status,
                    orderTimestamp = order.OrderTimestamp,
                    lastUpdated = order.LastUpdated
                }).ToList()
            });

            return response;
        }
    }
}
