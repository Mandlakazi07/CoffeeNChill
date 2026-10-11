using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class ProcessOrderQueue
    {
        private readonly OrderTableService _orderTableService;
        private readonly ILogger<ProcessOrderQueue> _logger;

        public ProcessOrderQueue(
            OrderTableService orderTableService,
            ILogger<ProcessOrderQueue> logger)
        {
            _orderTableService = orderTableService;
            _logger = logger;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger(
                "order-processing-queue",
                Connection = "AzureWebJobsStorage")]
            string queueMessage,
            FunctionContext context)
        {
            OrderQueueMessage? order =
                JsonSerializer.Deserialize<OrderQueueMessage>(
                    queueMessage,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (order == null ||
                string.IsNullOrWhiteSpace(order.OrderId))
            {
                throw new InvalidOperationException(
                    "The queue message did not contain a valid order.");
            }

            _logger.LogInformation(
                "Processing queued order {OrderId}.",
                order.OrderId);

            OrderEntity? existingOrder =
                await _orderTableService.GetOrderAsync(
                    order.OrderId,
                    context.CancellationToken);

            if (existingOrder == null)
            {
                await _orderTableService.CreateOrderAsync(
                    order,
                    context.CancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} stored with Received status.",
                    order.OrderId);
            }

            await _orderTableService.UpdateStatusAsync(
                order.OrderId,
                OrderStatus.Preparing,
                context.CancellationToken);

            _logger.LogInformation(
                "Order {OrderId} is being prepared.",
                order.OrderId);

            await _orderTableService.UpdateStatusAsync(
                order.OrderId,
                OrderStatus.Ready,
                context.CancellationToken);

            _logger.LogInformation(
                "Order {OrderId} is ready for collection.",
                order.OrderId);

            await _orderTableService.UpdateStatusAsync(
                order.OrderId,
                OrderStatus.Collected,
                context.CancellationToken);

            _logger.LogInformation(
                "Order {OrderId} has been collected.",
                order.OrderId);
        }
    }
}