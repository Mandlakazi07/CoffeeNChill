using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChillFunctions.Services
{
    public sealed class UsersTableService
    {
        private const string TableName = "Users";
        private readonly TableClient _tableClient;

        public UsersTableService(IConfiguration configuration)
        {
            string connectionString =
                configuration["AzureWebJobsStorage"]
                ?? "UseDevelopmentStorage=true";

            _tableClient = new TableClient(connectionString, TableName);
        }

        public async Task EnsureTableExistsAsync(CancellationToken cancellationToken = default)
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
        }

        public async Task<UserEntity?> GetUserAsync(string role, string email, CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);
            try
            {
                Response<UserEntity> response = await _tableClient.GetEntityAsync<UserEntity>(role, email.ToLowerInvariant(), cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddUserAsync(UserEntity user, CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);
            await _tableClient.AddEntityAsync(user, cancellationToken);
        }
    }
}