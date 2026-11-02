/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * QueueClient usage, message encoding and RequestFailedException handling follow Microsoft's Azure Queue Storage client guidance (Microsoft, n.d.-a; Microsoft, n.d.-b).
 * The CoffeeNChill producer contract and error mapping are project-specific; see docs/CODE_ATTRIBUTION.md.
 */

using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Services
{
    public sealed class OrderQueueService
    {
        public const string QueueName = "order-processing-queue";
        public const string MessageEncodingName = "base64";

        private readonly string? _connectionString;
        private readonly ILogger<OrderQueueService> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private QueueClient? _queueClient;

        public OrderQueueService(
            IConfiguration configuration,
            ILogger<OrderQueueService> logger)
        {
            // Read only; the client is built on first use so a bad or missing
            // connection string becomes a handled 503, not a DI failure.
            _connectionString = configuration["AzureWebJobsStorage"];
            _logger = logger;
        }

        public async Task<SendReceipt> QueueOrderAsync(
            OrderQueueMessage order,
            CancellationToken cancellationToken = default)
        {
            // Serializes, checks every key attribute is present and Base64
            // encodes. Throws OrderPayloadException before anything is sent.
            string encodedMessage = OrderQueueMessageCodec.EncodeToBase64(order);

            // Round-trip check: what the consumer will decode must match what
            // the producer meant to send.
            OrderQueueMessage roundTrip = OrderQueueMessageCodec.Decode(encodedMessage);

            if (roundTrip.OrderId != order.OrderId ||
                roundTrip.TotalPrice != order.TotalPrice ||
                roundTrip.SelectedItemSkus.Count != order.SelectedItemSkus.Count)
            {
                throw new OrderPayloadException(
                    $"Order '{order.OrderId}' did not survive Base64 round-trip encoding.");
            }

            QueueClient queueClient = await GetQueueClientAsync(cancellationToken);

            try
            {
                Response<SendReceipt> response = await queueClient.SendMessageAsync(
                    encodedMessage,
                    cancellationToken);

                return response.Value;
            }
            catch (RequestFailedException exception)
            {
                // A cached client may point at a queue that was deleted after
                // start-up; drop it so the next request re-creates the queue.
                _queueClient = null;

                throw new OrderQueueUnavailableException(
                    $"Azure Storage rejected the message (HTTP {exception.Status}, {exception.ErrorCode ?? "no error code"}).",
                    exception);
            }
            catch (AggregateException exception)
            {
                _queueClient = null;

                throw new OrderQueueUnavailableException(
                    $"Lost connection to queue '{QueueName}' while sending the message.",
                    exception);
            }
        }

        private async Task<QueueClient> GetQueueClientAsync(
            CancellationToken cancellationToken)
        {
            if (_queueClient != null)
            {
                return _queueClient;
            }

            await _initLock.WaitAsync(cancellationToken);

            try
            {
                if (_queueClient != null)
                {
                    return _queueClient;
                }

                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    throw new OrderQueueUnavailableException(
                        "AzureWebJobsStorage is not configured.");
                }

                QueueClient client;

                try
                {
                    // The codec already produces Base64 text, so the SDK must
                    // not encode it a second time. The queue trigger decodes
                    // Base64 (host.json: messageEncoding = base64).
                    client = new QueueClient(
                        _connectionString,
                        QueueName,
                        new QueueClientOptions
                        {
                            MessageEncoding = QueueMessageEncoding.None
                        });
                }
                catch (Exception exception) when (
                    exception is FormatException or ArgumentException)
                {
                    throw new OrderQueueUnavailableException(
                        "AzureWebJobsStorage is not a valid storage connection string.",
                        exception);
                }

                try
                {
                    await client.CreateIfNotExistsAsync(
                        cancellationToken: cancellationToken);
                }
                catch (RequestFailedException exception)
                {
                    throw new OrderQueueUnavailableException(
                        $"Could not create or reach queue '{QueueName}' (HTTP {exception.Status}, {exception.ErrorCode ?? "no error code"}).",
                        exception);
                }
                catch (AggregateException exception)
                {
                    // Raised by the SDK when every retry to the endpoint failed
                    // (for example Azurite not running).
                    throw new OrderQueueUnavailableException(
                        $"Could not connect to queue '{QueueName}'. Is the storage account (or Azurite) running?",
                        exception);
                }

                _logger.LogInformation(
                    "Connected to queue {QueueName} at {QueueUri}.",
                    QueueName,
                    client.Uri);

                _queueClient = client;
                return client;
            }
            finally
            {
                _initLock.Release();
            }
        }
    }

    public sealed class OrderQueueUnavailableException : Exception
    {
        public OrderQueueUnavailableException(string message)
            : base(message)
        {
        }

        public OrderQueueUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
