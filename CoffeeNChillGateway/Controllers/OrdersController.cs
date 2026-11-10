
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public OrdersController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Functions");
        }

        // Admin and Staff can view all orders.
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetOrders()
        {
            var response = await _httpClient.GetAsync(
                "orders",
                HttpContext.RequestAborted);

            return await ForwardResponseAsync(response);
        }

        // Authenticated users can check an order.
        [HttpGet("{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderStatus(string orderId)
        {
            string route = $"orders/{Uri.EscapeDataString(orderId)}";

            var response = await _httpClient.GetAsync(
                route,
                HttpContext.RequestAborted);

            return await ForwardResponseAsync(response);
        }

        // Authenticated users can check an order by date.
        [HttpGet("{orderDate}/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderStatusByDate(
            string orderDate,
            string orderId)
        {
            string route =
                $"orders/{Uri.EscapeDataString(orderDate)}/{Uri.EscapeDataString(orderId)}";

            var response = await _httpClient.GetAsync(
                route,
                HttpContext.RequestAborted);

            return await ForwardResponseAsync(response);
        }

        // Authenticated users can submit orders.
        [HttpPost("queue")]
        [Authorize]
        public async Task<IActionResult> QueueOrder(
            [FromBody] JsonElement orderRequest)
        {
            var content = new StringContent(
                orderRequest.GetRawText(),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "orders/queue",
                content,
                HttpContext.RequestAborted);

            return await ForwardResponseAsync(response);
        }

        // Only Admin and Staff can update order statuses.
        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateOrderStatus(
            string orderId,
            [FromBody] JsonElement statusRequest)
        {
            string route =
                $"orders/{Uri.EscapeDataString(orderId)}/status";

            var content = new StringContent(
                statusRequest.GetRawText(),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(
                route,
                content,
                HttpContext.RequestAborted);

            return await ForwardResponseAsync(response);
        }

        // Forward the Azure Functions response to the client.
        private static async Task<IActionResult> ForwardResponseAsync(
            HttpResponseMessage response)
        {
            string responseBody =
                await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = responseBody,
                ContentType =
                    response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
    }
}
