using System;
using System.Collections.Generic;

namespace CoffeeNChillFunctions.Models
{
    public sealed class OrderQueueMessage
    {
        public string OrderId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public List<string> SelectedItemSkus { get; set; } = new();

        public decimal TotalPrice { get; set; }

        public DateTimeOffset OrderTimestamp { get; set; }
    }
}