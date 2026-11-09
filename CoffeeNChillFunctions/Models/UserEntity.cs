using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions.Models
{
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; 
        public string RowKey { get; set; } = string.Empty;    
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}