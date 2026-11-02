/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * The Queue Storage producer/binding pattern follows Microsoft's published Azure Queue Storage guidance (Microsoft, n.d.-a; Microsoft, n.d.-b).
 * CoffeeNChill request validation, payload fields and response behaviour are project-specific adaptations. Shared-baseline and AI-assistance disclosures are in docs/CODE_ATTRIBUTION.md (EMKNDN, 2026; OpenAI, 2026).
 */

using System.Net;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Queues.Models;
using CoffeeNChillFunctions.DTOs;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Serialization;
using CoffeeNChillFunctions.Services;
using CoffeeNChillFunctions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions.Orders
{
    public sealed class QueueOrder
    {
        private const int MaxBodyBytes = 32 * 1024;
        private const int RetryAfterSeconds = 10;

        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            // Reject typos such as "totalprize" instead of silently dropping them.
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false
        };

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
            string? orderId = null;

            try
            {
                string body;

                using (StreamReader reader = new(request.Body))
                {
                    body = await reader.ReadToEndAsync(context.CancellationToken);
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.BadRequest,
                        "The request body is empty. A JSON order is required.");
                }

                if (System.Text.Encoding.UTF8.GetByteCount(body) > MaxBodyBytes)
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.RequestEntityTooLarge,
                        $"The request body cannot exceed {MaxBodyBytes / 1024} KB.");
                }

                OrderRequest? orderRequest =
                    JsonSerializer.Deserialize<OrderRequest>(body, RequestJsonOptions);

                if (orderRequest == null)
                {
                    return await CreateErrorResponse(
                        request,
                        HttpStatusCode.BadRequest,
                        "A valid order request is required.");
                }

                DateTimeOffset utcNow = DateTimeOffset.UtcNow;

                List<string> validationErrors =
                    OrderRequestValidator.Validate(orderRequest, utcNow);

                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning(
                        "Order {OrderId} failed validation: {Errors}",
                        orderRequest.OrderId ?? "(none)",
                        string.Join(" | ", validationErrors));

                    HttpResponseData validationResponse =
                        request.CreateResponse(HttpStatusCode.BadRequest);

                    await validationResponse.WriteAsJsonAsync(new
                    {
                        message = "Order validation failed.",
                        errors = validationErrors
                    });

                    return validationResponse;
                }

                List<string> skus = orderRequest.SelectedItemSkus!
                    .Select(sku => sku!.Trim())
                    .ToList();

                OrderQueueMessage queueMessage = new()
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    OrderId = orderRequest.OrderId!.Trim(),
                    CustomerName = orderRequest.CustomerName!.Trim(),
                    SelectedItemSkus = skus,
                    ItemCount = skus.Count,
                    TotalPrice = decimal.Round(orderRequest.TotalPrice!.Value, 2),
                    OrderTimestamp = (orderRequest.OrderTimestamp ?? utcNow).ToUniversalTime(),
                    QueuedAtUtc = utcNow
                };

                orderId = queueMessage.OrderId;

                SendReceipt receipt = await _queueService.QueueOrderAsync(
                    queueMessage,
                    context.CancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} (message {MessageId}, storage id {StorageMessageId}) was added to {QueueName}.",
                    queueMessage.OrderId,
                    queueMessage.MessageId,
                    receipt.MessageId,
                    OrderQueueService.QueueName);

                string orderDate = queueMessage.OrderTimestamp.UtcDateTime
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                QueueOrderResponse responseBody = new()
                {
                    Message = "Order accepted and placed on the processing queue.",
                    OrderId = queueMessage.OrderId,
                    Status = OrderStatus.Queued,
                    CustomerName = queueMessage.CustomerName,
                    SelectedItemSkus = queueMessage.SelectedItemSkus,
                    ItemCount = queueMessage.ItemCount,
                    TotalPrice = queueMessage.TotalPrice,
                    Currency = queueMessage.Currency,
                    OrderTimestamp = queueMessage.OrderTimestamp,
                    QueuedAtUtc = queueMessage.QueuedAtUtc,
                    OrderDate = orderDate,
                    StatusUrl = $"/api/orders/{orderDate}/{Uri.EscapeDataString(queueMessage.OrderId)}",
                    Queue = new QueueReceipt
                    {
                        Name = OrderQueueService.QueueName,
                        MessageId = queueMessage.MessageId,
                        StorageMessageId = receipt.MessageId,
                        InsertedOnUtc = receipt.InsertionTime,
                        Encoding = OrderQueueService.MessageEncodingName
                    }
                };

                HttpResponseData response =
                    request.CreateResponse(HttpStatusCode.Accepted);

                response.Headers.Add("Location", responseBody.StatusUrl);
                await response.WriteAsJsonAsync(responseBody);

                return response;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Invalid JSON was submitted to the order endpoint.");

                string detail = string.IsNullOrEmpty(exception.Path)
                    ? "The request body contains invalid JSON."
                    : $"The request body contains invalid JSON at '{exception.Path}'. Check the field name and value type.";

                return await CreateErrorResponse(
                    request,
                    HttpStatusCode.BadRequest,
                    detail);
            }
            catch (OrderQueueUnavailableException exception)
            {
                _logger.LogError(
                    exception,
                    "Order {OrderId} could not be queued because the queue is unavailable.",
                    orderId ?? "(unknown)");

                HttpResponseData response = await CreateErrorResponse(
                    request,
                    HttpStatusCode.ServiceUnavailable,
                    "The order queue is temporarily unavailable. Please try again shortly.");

                response.Headers.Add(
                    "Retry-After",
                    RetryAfterSeconds.ToString(CultureInfo.InvariantCulture));

                return response;
            }
            catch (OrderPayloadException exception)
            {
                // Should not happen after validation; guards against a
                // malformed message ever reaching the queue.
                _logger.LogError(
                    exception,
                    "Order {OrderId} produced a malformed queue payload and was not sent.",
                    orderId ?? "(unknown)");

                return await CreateErrorResponse(
                    request,
                    HttpStatusCode.InternalServerError,
                    "The order could not be queued because its payload was incomplete.");
            }
            catch (OperationCanceledException) when (
                context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Queuing of order {OrderId} was cancelled before it completed.",
                    orderId ?? "(unknown)");

                return await CreateErrorResponse(
                    request,
                    HttpStatusCode.ServiceUnavailable,
                    "The request was cancelled before the order was queued.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while queuing order {OrderId}.",
                    orderId ?? "(unknown)");

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

            await response.WriteAsJsonAsync(
                new
                {
                    error = message
                });

            return response;
        }
    }
}
