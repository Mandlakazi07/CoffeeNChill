using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Services;

public sealed class StaffFileService
{
    private const long MaximumFileSize =
        10 * 1024 * 1024;

    private readonly BlobContainerClient _container;
    private readonly ILogger<StaffFileService> _logger;

    public StaffFileService(
        BlobContainerClient container,
        ILogger<StaffFileService> logger)
    {
        _container = container;
        _logger = logger;
    }

    private async Task EnsureContainerAsync(
        CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: ct);
    }

    public async Task<StaffDocument> UploadAsync(
        string fileName,
        Stream content,
        string mime = "application/pdf",
        CancellationToken ct = default)
    {
        ValidateFileName(fileName);

        await using MemoryStream uploadStream =
            await PreparePdfStreamAsync(content, ct);

        long length = uploadStream.Length;

        ValidateFileInformation(length, mime);

        await EnsureContainerAsync(ct);

        BlobClient blob =
            _container.GetBlobClient(fileName);

        try
        {
            await blob.UploadAsync(
                uploadStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "application/pdf"
                    },

                    Metadata = new Dictionary<string, string>
                    {
                        ["uploadedutc"] =
                            DateTimeOffset.UtcNow.ToString("O")
                    },

                    Conditions = new BlobRequestConditions
                    {
                        IfNoneMatch = ETag.All
                    }
                },
                ct);
        }
        catch (RequestFailedException exception)
            when (exception.Status is 409 or 412)
        {
            throw new InvalidOperationException(
                "A file with that name already exists.",
                exception);
        }

        BlobProperties properties =
            (await blob.GetPropertiesAsync(
                cancellationToken: ct)).Value;

        _logger.LogInformation(
            "Uploaded staff document {FileName}, {Bytes} bytes",
            fileName,
            properties.ContentLength);

        return CreateStaffDocument(
            fileName,
            properties.ContentLength,
            properties.LastModified,
            properties.ContentType);
    }

    public async Task<List<StaffDocument>> ListAsync(
        CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);

        List<StaffDocument> documents = new();

        await foreach (
            BlobItem item in
            _container.GetBlobsAsync(
                cancellationToken: ct))
        {
            StaffDocument document =
                CreateStaffDocument(
                    item.Name,
                    item.Properties.ContentLength ?? 0,
                    item.Properties.LastModified
                        ?? DateTimeOffset.MinValue,
                    item.Properties.ContentType);

            documents.Add(document);
        }

        documents.Sort(
            (first, second) =>
                StringComparer.OrdinalIgnoreCase.Compare(
                    first.FileName,
                    second.FileName));

        return documents;
    }

    public async Task<(Stream Content, string ContentType)?>
        DownloadAsync(
            string fileName,
            CancellationToken ct = default)
    {
        ValidateFileName(fileName);

        await EnsureContainerAsync(ct);

        BlobClient blob =
            _container.GetBlobClient(fileName);

        try
        {
            BlobDownloadStreamingResult download =
                (await blob.DownloadStreamingAsync(
                    cancellationToken: ct)).Value;

            _logger.LogInformation(
                "Downloaded staff document {FileName}",
                fileName);

            return (
                download.Content,
                download.Details.ContentType
                    ?? "application/pdf");
        }
        catch (RequestFailedException exception)
            when (exception.Status == 404)
        {
            _logger.LogWarning(
                "Staff document {FileName} was not found",
                fileName);

            return null;
        }
    }

    private static StaffDocument CreateStaffDocument(
        string fileName,
        long fileSize,
        DateTimeOffset lastModified,
        string? contentType)
    {
        return new StaffDocument
        {
            FileName = fileName,
            FileSize = fileSize,
            LastModified = lastModified,

            ContentType =
                contentType ?? "application/pdf",

            DownloadUrl =
                "/api/documents/download/" +
                Uri.EscapeDataString(fileName)
        };
    }

    private static void ValidateFileName(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A file name is required.",
                nameof(fileName));
        }

        if (!string.Equals(
                fileName,
                Path.GetFileName(fileName),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The file name is invalid.",
                nameof(fileName));
        }

        if (!string.Equals(
                Path.GetExtension(fileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only PDF documents are allowed.",
                nameof(fileName));
        }
    }

    private static void ValidateFileInformation(
        long length,
        string mime)
    {
        if (length <= 0)
        {
            throw new ArgumentException(
                "The uploaded file is empty.");
        }

        if (length > MaximumFileSize)
        {
            throw new ArgumentException(
                "The PDF cannot exceed 10 MB.");
        }

        if (!string.IsNullOrWhiteSpace(mime) &&
            !mime.Equals(
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only application/pdf files are allowed.");
        }
    }

    private static async Task<MemoryStream>
        PreparePdfStreamAsync(
            Stream source,
            CancellationToken ct)
    {
        MemoryStream copy = new();

        if (source.CanSeek)
        {
            source.Position = 0;
        }

        await source.CopyToAsync(copy, ct);

        if (copy.Length < 5)
        {
            await copy.DisposeAsync();

            throw new ArgumentException(
                "The uploaded file is not a valid PDF.");
        }

        copy.Position = 0;

        byte[] signature = new byte[5];

        int bytesRead =
            await copy.ReadAsync(
                signature.AsMemory(0, 5),
                ct);

        bool isPdf =
            bytesRead == 5 &&
            signature[0] == (byte)'%' &&
            signature[1] == (byte)'P' &&
            signature[2] == (byte)'D' &&
            signature[3] == (byte)'F' &&
            signature[4] == (byte)'-';

        if (!isPdf)
        {
            await copy.DisposeAsync();

            throw new ArgumentException(
                "The uploaded file is not a valid PDF.");
        }

        copy.Position = 0;

        return copy;
    }
}