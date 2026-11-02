/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * Base64 queue message handling follows Microsoft's Azure Queue Storage and Azure Functions queue trigger guidance (Microsoft, n.d.-a; Microsoft, n.d.-b).
 * The CoffeeNChill message contract and payload checks are project-specific; see docs/CODE_ATTRIBUTION.md.
 */

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Serialization
{
    /// <summary>
    /// Single place that turns an order into the queue payload and back.
    /// Encode: object -> camelCase JSON -> UTF-8 bytes -> Base64 text.
    /// Decode: Base64 text (or raw JSON) -> JSON -> object, then checks
    /// that every required attribute is present.
    /// </summary>
    public static class OrderQueueMessageCodec
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = false
        };

        public static string SerializeToJson(OrderQueueMessage message)
        {
            EnsureComplete(message);
            return JsonSerializer.Serialize(message, JsonOptions);
        }

        public static string EncodeToBase64(OrderQueueMessage message)
        {
            string json = SerializeToJson(message);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Accepts either the Base64 text stored on the queue or the JSON
        /// the Functions runtime hands to a queue trigger after it has
        /// already decoded the Base64 layer.
        /// </summary>
        public static OrderQueueMessage Decode(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new OrderPayloadException("The queue message was empty.");
            }

            string trimmed = payload.Trim();
            string json = trimmed.StartsWith('{')
                ? trimmed
                : DecodeBase64(trimmed);

            OrderQueueMessage? message;

            try
            {
                message = JsonSerializer.Deserialize<OrderQueueMessage>(
                    json,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new OrderPayloadException(
                    "The queue message is not valid order JSON.",
                    exception);
            }

            if (message == null)
            {
                throw new OrderPayloadException(
                    "The queue message did not contain an order.");
            }

            // Messages queued before schema version 1 had no envelope fields.
            // Fill them so those orders can still be processed; the order
            // attributes themselves are still strictly checked below.
            if (string.IsNullOrWhiteSpace(message.MessageId))
            {
                message.MessageId = Guid.NewGuid().ToString("N");
            }

            if (message.QueuedAtUtc == default)
            {
                message.QueuedAtUtc = message.OrderTimestamp;
            }

            EnsureComplete(message);
            return message;
        }

        /// <summary>
        /// Throws if any key attribute is missing, so a malformed order can
        /// never be written to, or accepted from, the queue.
        /// </summary>
        public static void EnsureComplete(OrderQueueMessage message)
        {
            List<string> missing = new();

            if (string.IsNullOrWhiteSpace(message.MessageId))
            {
                missing.Add("messageId");
            }

            if (string.IsNullOrWhiteSpace(message.OrderId))
            {
                missing.Add("orderId");
            }

            if (string.IsNullOrWhiteSpace(message.CustomerName))
            {
                missing.Add("customerName");
            }

            if (message.SelectedItemSkus == null ||
                message.SelectedItemSkus.Count == 0 ||
                message.SelectedItemSkus.Any(string.IsNullOrWhiteSpace))
            {
                missing.Add("selectedItemSkus");
            }

            if (message.TotalPrice <= 0)
            {
                missing.Add("totalPrice");
            }

            if (message.OrderTimestamp == default)
            {
                missing.Add("orderTimestamp");
            }

            if (message.QueuedAtUtc == default)
            {
                missing.Add("queuedAtUtc");
            }

            if (missing.Count > 0)
            {
                throw new OrderPayloadException(
                    "The order payload is missing or has invalid values for: " +
                    string.Join(", ", missing) + ".");
            }
        }

        private static string DecodeBase64(string base64)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException exception)
            {
                throw new OrderPayloadException(
                    "The queue message is neither JSON nor valid Base64.",
                    exception);
            }
        }
    }

    public sealed class OrderPayloadException : Exception
    {
        public OrderPayloadException(string message)
            : base(message)
        {
        }

        public OrderPayloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
