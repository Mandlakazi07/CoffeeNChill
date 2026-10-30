/* 
 * Code Attribution:
 * Title: Azure Table Storage Entity
 * Author: Microsoft Learn
 * Date: 30 October 2026
 */

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Models
{
    public class UserProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTimeOffset? CreatedDate { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
