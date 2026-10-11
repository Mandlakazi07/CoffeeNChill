using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChillFunctions.Services
{
    public sealed class OrderQueueService
    {
        private const string QueueName = "order-processing-queue";
        private readonly QueueClient _queueClient;

        public OrderQueueService(IConfiguration configuration)
        {
            string connectionString =
                configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is not configured.");

            QueueClientOptions options = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            _queueClient = new QueueClient(
                connectionString,
                QueueName,
                options);
        }

        public async Task QueueOrderAsync(
            OrderQueueMessage order,
            CancellationToken cancellationToken = default)
        {
            await _queueClient.CreateIfNotExistsAsync(
                cancellationToken: cancellationToken);

            string messageJson = JsonSerializer.Serialize(order);

            await _queueClient.SendMessageAsync(
                messageJson,
                cancellationToken);
        }
    }
}
