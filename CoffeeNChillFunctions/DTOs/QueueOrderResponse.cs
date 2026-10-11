namespace CoffeeNChillFunctions.DTOs
{
    public sealed class QueueOrderResponse
    {
        public string OrderId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string OrderDate { get; set; } = string.Empty;

        public string StatusUrl { get; set; } = string.Empty;
    }
}
