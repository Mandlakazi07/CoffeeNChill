using System;
using System.Collections.Generic;

namespace CoffeeNChillFunctions.DTOs
{
    public sealed class OrderRequest
    {
        public string? OrderId { get; set; }

        public string? CustomerName { get; set; }

        public List<string?>? SelectedItemSkus { get; set; }

        // Nullable so a missing price can be told apart from a zero price.
        public decimal? TotalPrice { get; set; }

        public DateTimeOffset? OrderTimestamp { get; set; }
    }
}
