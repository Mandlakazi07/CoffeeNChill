using Azure;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeNChillFunctions.Functions.Documents
{
    public class DownloadStaffDocument
    {
        private readonly StaffFileService _staffFileService;

        public DownloadStaffDocument(
            StaffFileService staffFileService)
        {
            _staffFileService = staffFileService;
        }

        [Function("DownloadStaffDocument")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents/download/{fileName}")]
            HttpRequest request,
            string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "A file name is required."
                    });
                }

                fileName = Uri.UnescapeDataString(fileName);

                if (fileName != Path.GetFileName(fileName) ||
                    fileName.Contains("..") ||
                    fileName.Contains('/') ||
                    fileName.Contains('\\'))
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "The file name is invalid."
                    });
                }

                (Stream Content, string ContentType)? download =
                    await _staffFileService.DownloadAsync(fileName);

                if (download == null)
                {
                    return new NotFoundObjectResult(new
                    {
                        error = $"Document '{fileName}' was not found."
                    });
                }

                return new FileStreamResult(
                    download.Value.Content,
                    download.Value.ContentType)
                {
                    FileDownloadName = fileName
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
                        $"Unable to download document: {exception.Message}"
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}