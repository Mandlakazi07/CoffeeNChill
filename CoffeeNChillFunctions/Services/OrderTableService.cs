/*
 * CODE ATTRIBUTION (IIE Harvard Anglia)
 * Table access uses the Azure.Data.Tables client API documented by Microsoft (Microsoft, n.d.-d).
 * CoffeeNChill entity keys, status rules and date-based query behaviour are project-specific; source lineage and AI-assisted revisions are disclosed in docs/CODE_ATTRIBUTION.md (EMKNDN, 2026; OpenAI, 2026).
 */

using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChillFunctions.Services
{
    public sealed class OrderTableService
    {
        private const string TableName = "Orders";

        private readonly TableClient _tableClient;

        public OrderTableService(IConfiguration configuration)
        {
            string connectionString =
                configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is not configured.");

            _tableClient = new TableClient(
                connectionString,
                TableName);
        }

        public async Task CreateOrderAsync(
            OrderQueueMessage order,
            CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);

            OrderEntity entity = new()
            {
                PartitionKey = order.OrderTimestamp.UtcDateTime.ToString("yyyy-MM-dd"),
                RowKey = order.OrderId,
                CustomerName = order.CustomerName,
                SelectedItemSkusJson =
                    JsonSerializer.Serialize(order.SelectedItemSkus),
                TotalPrice = Convert.ToDouble(order.TotalPrice),
                Status = OrderStatus.Received,
                OrderTimestamp = order.OrderTimestamp,
                LastUpdated = DateTimeOffset.UtcNow
            };

            await _tableClient.AddEntityAsync(
                entity,
                cancellationToken);
        }

        public async Task<OrderEntity?> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);

            await foreach (OrderEntity entity in
                _tableClient.QueryAsync<OrderEntity>(
                    order => order.RowKey == orderId,
                    cancellationToken: cancellationToken))
            {
                return entity;
            }

            return null;
        }

        public async Task UpdateStatusAsync(
            string orderId,
            string newStatus,
            CancellationToken cancellationToken = default)
        {
            OrderEntity? entity =
                await GetOrderAsync(orderId, cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException(
                    $"Order '{orderId}' was not found.");
            }

            entity.Status = newStatus;
            entity.LastUpdated = DateTimeOffset.UtcNow;

            await _tableClient.UpdateEntityAsync(
                entity,
                ETag.All,
                TableUpdateMode.Replace,
                cancellationToken);
        }

        public async Task<OrderEntity?> GetOrderAsync(
            string orderDate,
            string orderId,
            CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);

            await foreach (OrderEntity entity in
                _tableClient.QueryAsync<OrderEntity>(
                    order => order.PartitionKey == orderDate &&
                             order.RowKey == orderId,
                    cancellationToken: cancellationToken))
            {
                return entity;
            }

            return null;
        }

        public async Task<List<OrderEntity>> GetOrdersByDateAsync(
            string orderDate,
            CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);

            List<OrderEntity> orders = new();

            await foreach (OrderEntity entity in
                _tableClient.QueryAsync<OrderEntity>(
                    order => order.PartitionKey == orderDate,
                    cancellationToken: cancellationToken))
            {
                orders.Add(entity);
            }

            return orders
                .OrderByDescending(order => order.OrderTimestamp)
                .ToList();
        }

        public async Task<List<OrderEntity>> GetAllOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            await EnsureTableExistsAsync(cancellationToken);

            List<OrderEntity> orders = new();

            await foreach (OrderEntity entity in
                _tableClient.QueryAsync<OrderEntity>(
                    cancellationToken: cancellationToken))
            {
                orders.Add(entity);
            }

            return orders
                .OrderByDescending(order => order.OrderTimestamp)
                .ToList();
        }

        private async Task EnsureTableExistsAsync(
            CancellationToken cancellationToken)
        {
            await _tableClient.CreateIfNotExistsAsync(
                cancellationToken);
        }
    }
}