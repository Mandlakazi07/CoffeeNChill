using Azure.Storage.Blobs;
using CoffeeNChillFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register the existing menu service.
builder.Services.AddSingleton<MenuTableService>();
builder.Services.AddSingleton<OrderQueueService>();
builder.Services.AddSingleton<OrderTableService>();

// Register the Azurite Blob Storage service.
builder.Services.AddSingleton<StaffFileService>(
    serviceProvider =>
    {
        IConfiguration configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string connectionString =
            configuration["StaffBlobConnection"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "StaffBlobConnection or AzureWebJobsStorage is missing.");

        string containerName =
            configuration["StaffBlobContainer"]
            ?? "staff-docs";

        BlobContainerClient containerClient =
            new BlobContainerClient(
                connectionString,
                containerName);

        ILogger<StaffFileService> logger =
            serviceProvider.GetRequiredService<
                ILogger<StaffFileService>>();

        return new StaffFileService(
            containerClient,
            logger);
    });

builder.Build().Run();