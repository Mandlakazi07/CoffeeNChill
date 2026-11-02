using System.Text.Json.Serialization;

namespace CoffeeNChillFunctions.DTOs
{
    /// <summary>
    /// 202 response for POST /api/orders/queue. Echoes the exact order that
    /// was placed on the queue so the client can confirm every attribute.
    /// </summary>
    public sealed class QueueOrderResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("selectedItemSkus")]
        public List<string> SelectedItemSkus { get; set; } = new();

        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("orderTimestamp")]
        public DateTimeOffset OrderTimestamp { get; set; }

        [JsonPropertyName("queuedAtUtc")]
        public DateTimeOffset QueuedAtUtc { get; set; }

        [JsonPropertyName("orderDate")]
        public string OrderDate { get; set; } = string.Empty;

        [JsonPropertyName("statusUrl")]
        public string StatusUrl { get; set; } = string.Empty;

        [JsonPropertyName("queue")]
        public QueueReceipt Queue { get; set; } = new();
    }

    public sealed class QueueReceipt
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("storageMessageId")]
        public string StorageMessageId { get; set; } = string.Empty;

        [JsonPropertyName("insertedOnUtc")]
        public DateTimeOffset InsertedOnUtc { get; set; }

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = string.Empty;
    }
}
