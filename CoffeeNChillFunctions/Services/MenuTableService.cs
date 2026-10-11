using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChillFunctions.Services
{
    public class MenuTableService
    {
        private const string TableName = "MenuItems";
        private readonly TableClient _tableClient;

        public MenuTableService(IConfiguration configuration)
        {
            string? configuredConnectionString =
                configuration["AzureWebJobsStorage"];

            string connectionString =
                string.IsNullOrWhiteSpace(configuredConnectionString)
                    ? "UseDevelopmentStorage=true"
                    : configuredConnectionString;

            _tableClient = new TableClient(
                connectionString,
                TableName);
        }

        private async Task EnsureTableExistsAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();
        }

        public async Task AddMenuItemAsync(MenuItem menuItem)
        {
            await EnsureTableExistsAsync();
            await _tableClient.AddEntityAsync(menuItem);
        }

        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            await EnsureTableExistsAsync();

            List<MenuItem> menuItems = new();

            await foreach (MenuItem item in
                _tableClient.QueryAsync<MenuItem>())
            {
                menuItems.Add(item);
            }

            return menuItems;
        }

        public async Task<List<MenuItem>>
            GetMenuItemsByCategoryAsync(string category)
        {
            await EnsureTableExistsAsync();

            List<MenuItem> menuItems = new();

            string filter =
                TableClient.CreateQueryFilter<MenuItem>(
                    item => item.PartitionKey == category);

            await foreach (MenuItem item in
                _tableClient.QueryAsync<MenuItem>(filter))
            {
                menuItems.Add(item);
            }

            return menuItems;
        }

        public async Task<MenuItem?> GetMenuItemAsync(
            string category,
            string id)
        {
            await EnsureTableExistsAsync();

            try
            {
                Response<MenuItem> response =
                    await _tableClient.GetEntityAsync<MenuItem>(
                        category,
                        id);

                return response.Value;
            }
            catch (RequestFailedException exception)
                when (exception.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateMenuItemAsync(MenuItem menuItem)
        {
            await EnsureTableExistsAsync();

            await _tableClient.UpdateEntityAsync(
                menuItem,
                menuItem.ETag,
                TableUpdateMode.Replace);
        }

        public async Task DeleteMenuItemAsync(
            string category,
            string id)
        {
            await EnsureTableExistsAsync();

            await _tableClient.DeleteEntityAsync(
                category,
                id,
                ETag.All);
        }
    }
}