/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * Queue-trigger execution, retry timing and poison-message behaviour follow Microsoft Azure Functions guidance (Microsoft, n.d.-a; Microsoft, n.d.-b).
 * The CoffeeNChill lifecycle and idempotency logic are project-specific adaptations; see docs/CODE_ATTRIBUTION.md for the shared baseline, technical references and AI-use disclosure (EMKNDN, 2026; OpenAI, 2026).
 */

using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class ProcessOrderQueue
    {
        private const int MaxDequeueCount = 3;
        private const string PoisonQueueName = "order-processing-queue-poison";

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
            string? orderId = null;

            try
            {
                if (string.IsNullOrWhiteSpace(queueMessage))
                {
                    throw new InvalidOperationException(
                        "The queue message was empty.");
                }

                OrderQueueMessage? order =
                    JsonSerializer.Deserialize<OrderQueueMessage>(
                        queueMessage,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (order == null || string.IsNullOrWhiteSpace(order.OrderId))
                {
                    throw new InvalidOperationException(
                        "The queue message did not contain a valid order ID.");
                }

                orderId = order.OrderId;

                _logger.LogInformation(
                    "Processing queued order {OrderId}.",
                    order.OrderId);

                OrderEntity? existingOrder =
                    await _orderTableService.GetOrderAsync(
                        order.OrderId,
                        context.CancellationToken);

                string currentStatus;

                if (existingOrder == null)
                {
                    await _orderTableService.CreateOrderAsync(
                        order,
                        context.CancellationToken);

                    currentStatus = OrderStatus.Received;
                    _logger.LogInformation(
                        "Order {OrderId} stored with Received status.",
                        order.OrderId);
                }
                else
                {
                    currentStatus = existingOrder.Status;

                    if (currentStatus == OrderStatus.Collected)
                    {
                        _logger.LogWarning(
                            "Ignoring duplicate queue message for already collected order {OrderId}.",
                            order.OrderId);
                        return;
                    }
                }

                // Resume from the last persisted status rather than moving an
                // order backwards if a message is delivered more than once.
                if (currentStatus == OrderStatus.Received)
                {
                    await _orderTableService.UpdateStatusAsync(
                        order.OrderId,
                        OrderStatus.Preparing,
                        context.CancellationToken);

                    currentStatus = OrderStatus.Preparing;
                    _logger.LogInformation(
                        "Order {OrderId} is being prepared.",
                        order.OrderId);
                }

                if (currentStatus == OrderStatus.Preparing)
                {
                    await _orderTableService.UpdateStatusAsync(
                        order.OrderId,
                        OrderStatus.Ready,
                        context.CancellationToken);

                    currentStatus = OrderStatus.Ready;
                    _logger.LogInformation(
                        "Order {OrderId} is ready for collection.",
                        order.OrderId);
                }

                if (currentStatus == OrderStatus.Ready)
                {
                    await _orderTableService.UpdateStatusAsync(
                        order.OrderId,
                        OrderStatus.Collected,
                        context.CancellationToken);

                    currentStatus = OrderStatus.Collected;
                    _logger.LogInformation(
                        "Order {OrderId} has been collected.",
                        order.OrderId);
                }

                if (currentStatus != OrderStatus.Collected)
                {
                    throw new InvalidOperationException(
                        $"Order '{order.OrderId}' has an unsupported status '{currentStatus}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process queued order {OrderId}. The Functions runtime will retry it; after {MaxDequeueCount} failed attempts, the message should be moved to {PoisonQueueName}.",
                    orderId ?? "(unknown)",
                    MaxDequeueCount,
                    PoisonQueueName);

                // Do not swallow this exception. The queue trigger must see a
                // failed invocation so it can retry and eventually poison it.
                throw;
            }
        }
    }
}
