using Azure;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeNChillFunctions.Functions.Documents
{
    public class UploadStaffDocument
    {
        private const long MaximumFileSize = 10 * 1024 * 1024;

        private readonly StaffFileService _staffFileService;

        public UploadStaffDocument(
            StaffFileService staffFileService)
        {
            _staffFileService = staffFileService;
        }

        [Function("UploadStaffDocument")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "documents/upload")]
            HttpRequest request)
        {
            try
            {
                if (!request.HasFormContentType)
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "The request must use multipart/form-data."
                    });
                }

                IFormCollection form =
                    await request.ReadFormAsync();

                IFormFile? uploadedFile =
                    form.Files.GetFile("file");

                if (uploadedFile == null ||
                    uploadedFile.Length == 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        error =
                            "Select a document using the form field 'file'."
                    });
                }

                if (uploadedFile.Length > MaximumFileSize)
                {
                    return new BadRequestObjectResult(new
                    {
                        error =
                            "The document may not exceed 10 MB."
                    });
                }

                string fileName =
                    Path.GetFileName(uploadedFile.FileName);

                if (string.IsNullOrWhiteSpace(fileName) ||
                    fileName.Contains(".."))
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "The file name is invalid."
                    });
                }

                string[] allowedExtensions =
                {
                    ".pdf"
                };

                string extension =
                    Path.GetExtension(fileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return new BadRequestObjectResult(new
                    {
                        error =
                            "Only PDF documents are supported by the current storage service."
                    });
                }

                string contentType =
                    string.IsNullOrWhiteSpace(
                        uploadedFile.ContentType)
                    ? "application/octet-stream"
                    : uploadedFile.ContentType;

                await using Stream content =
                    uploadedFile.OpenReadStream();

                StaffDocument document =
                    await _staffFileService.UploadAsync(
                        fileName,
                        content,
                        contentType);

                return new ObjectResult(document)
                {
                    StatusCode =
                        StatusCodes.Status201Created
                };
            }
            catch (RequestFailedException exception)
            {
                return new ObjectResult(new
                {
                    error =
                        $"Azure File Share error: {exception.Message}"
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
            catch (Exception exception)
            {
                return new ObjectResult(new
                {
                    error =
                        $"Unable to upload document: {exception.Message}"
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
