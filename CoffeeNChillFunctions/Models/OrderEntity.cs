using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions.Models
{
    public sealed class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "ORDER";
        public string RowKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        public string SelectedItemSkusJson { get; set; } = "[]";

        // Azure Table Storage does not support decimal values directly.
        public double TotalPrice { get; set; }

        public string Status { get; set; } = OrderStatus.Received;
        public DateTimeOffset OrderTimestamp { get; set; }
        public DateTimeOffset LastUpdated { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}