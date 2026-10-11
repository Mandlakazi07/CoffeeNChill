using Azure;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeNChillFunctions.Functions.Documents
{
    public class ListStaffDocuments
    {
        private readonly StaffFileService _staffFileService;

        public ListStaffDocuments(
            StaffFileService staffFileService)
        {
            _staffFileService = staffFileService;
        }

        [Function("ListStaffDocuments")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents")]
            HttpRequest request)
        {
            try
            {
                List<StaffDocument> documents =
                    await _staffFileService.ListAsync();

                return new OkObjectResult(new
                {
                    count = documents.Count,
                    documents
                });
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
                        $"Unable to retrieve documents: {exception.Message}"
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}