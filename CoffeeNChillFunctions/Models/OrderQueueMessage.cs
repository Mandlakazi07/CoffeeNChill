using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoffeeNChillFunctions.Models
{
    /// <summary>
    /// The JSON contract placed on order-processing-queue. Property names are
    /// pinned with JsonPropertyName so the payload shape never depends on
    /// serializer defaults.
    /// </summary>
    public sealed class OrderQueueMessage
    {
        public const int CurrentSchemaVersion = 1;

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("selectedItemSkus")]
        public List<string> SelectedItemSkus { get; set; } = new();

        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "ZAR";

        // When the customer placed the order (client supplied, or the
        // server's UTC time if the client left it out).
        [JsonPropertyName("orderTimestamp")]
        public DateTimeOffset OrderTimestamp { get; set; }

        // When the API accepted the order and placed it on the queue.
        [JsonPropertyName("queuedAtUtc")]
        public DateTimeOffset QueuedAtUtc { get; set; }
    }
}
