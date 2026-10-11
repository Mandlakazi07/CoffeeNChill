namespace CoffeeNChillFunctions.Models
{
    public class StaffDocument
    {
        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } =
            "application/octet-stream";

        public long FileSize { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public string DownloadUrl { get; set; } = string.Empty;
    }
}
