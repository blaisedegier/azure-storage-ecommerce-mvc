using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Data.Tables;
using BasicEcom.Models;
using Microsoft.AspNetCore.Identity;

namespace BasicEcom.Services
{
    // Reads and writes customers, products and orders in Azure Table Storage, and handles sign-in.
    // Adapted from https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableserviceclient?view=azure-dotnet
    // Adapted from https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient?view=azure-dotnet
    // Adapted from https://www.c-sharpcorner.com/article/net-core-crud-operation-using-azure-table-api/
    public class TableService
    {
        private static readonly PasswordHasher<Customers> PasswordHasher = new();

        private readonly TableClient _customersTableClient;
        private readonly TableClient _productsTableClient;
        private readonly TableClient _ordersTableClient;

        public TableService(string connectionString, string customersTableName, string productsTableName, string ordersTableName)
        {
            var tableServiceClient = new TableServiceClient(connectionString);

            _customersTableClient = tableServiceClient.GetTableClient(customersTableName);
            _customersTableClient.CreateIfNotExists();

            _productsTableClient = tableServiceClient.GetTableClient(productsTableName);
            _productsTableClient.CreateIfNotExists();

            _ordersTableClient = tableServiceClient.GetTableClient(ordersTableName);
            _ordersTableClient.CreateIfNotExists();
        }

        #region Customers
        public Task AddCustomerAsync(Customers customer) =>
            _customersTableClient.AddEntityAsync(customer);

        public Task<Customers?> FindCustomerAsync(string partitionKey, string rowKey) =>
            FindAsync<Customers>(_customersTableClient, partitionKey, rowKey);

        public async Task<Customers?> FindCustomerByEmailAsync(string email)
        {
            var filter = TableClient.CreateQueryFilter<Customers>(c => c.Email == email);
            await foreach (var customer in _customersTableClient.QueryAsync<Customers>(filter, maxPerPage: 1))
            {
                return customer;
            }
            return null;
        }

        public Task UpdateCustomerAsync(Customers customer) =>
            _customersTableClient.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);

        public Task DeleteCustomerAsync(string partitionKey, string rowKey) =>
            _customersTableClient.DeleteEntityAsync(partitionKey, rowKey);

        // Adapted from https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.query?view=azure-dotnet
        public Task<List<Customers>> QueryCustomersAsync() => QueryAllAsync<Customers>(_customersTableClient);
        #endregion

        // Adapted from https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction?view=aspnetcore-8.0
        #region Authentication
        // Returns the customer with a fresh session token when the email and password match, otherwise null.
        public async Task<Customers?> AuthenticateCustomerAsync(string email, string password)
        {
            var customer = await FindCustomerByEmailAsync(email).ConfigureAwait(false);
            if (customer?.PasswordHash == null || !VerifyPassword(customer, password))
            {
                return null;
            }

            customer.SessionToken = GenerateSessionToken();
            customer.LastLogin = DateTimeOffset.UtcNow;
            await UpdateCustomerAsync(customer).ConfigureAwait(false);
            return customer;
        }

        public async Task LogoutCustomerAsync(string partitionKey, string rowKey)
        {
            var customer = await FindCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);
            if (customer != null)
            {
                customer.SessionToken = null;
                await UpdateCustomerAsync(customer).ConfigureAwait(false);
            }
        }

        // PBKDF2 with a per-password random salt, via ASP.NET Core Identity's PasswordHasher.
        public static string HashPassword(Customers customer, string password) =>
            PasswordHasher.HashPassword(customer, password);

        // Earlier versions stored an unsalted SHA-256 hash. Such a hash is still accepted once and replaced
        // with a PasswordHasher hash, which AuthenticateCustomerAsync then saves.
        private static bool VerifyPassword(Customers customer, string password)
        {
            PasswordVerificationResult result;
            try
            {
                result = PasswordHasher.VerifyHashedPassword(customer, customer.PasswordHash!, password);
            }
            catch (FormatException)
            {
                result = PasswordVerificationResult.Failed;
            }
            if (result == PasswordVerificationResult.Success)
            {
                return true;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded || IsLegacySha256Match(customer.PasswordHash!, password))
            {
                customer.PasswordHash = HashPassword(customer, password);
                return true;
            }

            return false;
        }

        private static bool IsLegacySha256Match(string storedHash, string password)
        {
            var legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(legacyHash), Encoding.UTF8.GetBytes(storedHash));
        }

        private static string GenerateSessionToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        #endregion

        #region Products
        public Task AddProductAsync(Products product) =>
            _productsTableClient.AddEntityAsync(product);

        public Task<Products?> FindProductAsync(string partitionKey, string rowKey) =>
            FindAsync<Products>(_productsTableClient, partitionKey, rowKey);

        public Task UpdateProductAsync(Products product) =>
            _productsTableClient.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

        public Task DeleteProductAsync(string partitionKey, string rowKey) =>
            _productsTableClient.DeleteEntityAsync(partitionKey, rowKey);

        public Task<List<Products>> QueryProductsAsync() => QueryAllAsync<Products>(_productsTableClient);
        #endregion

        #region Orders
        public Task AddOrderAsync(Orders order) =>
            _ordersTableClient.AddEntityAsync(order);

        public Task<Orders?> FindOrderAsync(string partitionKey, string rowKey) =>
            FindAsync<Orders>(_ordersTableClient, partitionKey, rowKey);

        public Task UpdateOrderAsync(Orders order) =>
            _ordersTableClient.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);

        public Task DeleteOrderAsync(string partitionKey, string rowKey) =>
            _ordersTableClient.DeleteEntityAsync(partitionKey, rowKey);

        public Task<List<Orders>> QueryOrdersAsync() => QueryAllAsync<Orders>(_ordersTableClient);
        #endregion

        private static async Task<T?> FindAsync<T>(TableClient table, string partitionKey, string rowKey)
            where T : class, ITableEntity, new()
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return null;
            }

            NullableResponse<T> response = await table.GetEntityIfExistsAsync<T>(partitionKey, rowKey).ConfigureAwait(false);
            return response.HasValue ? response.Value : null;
        }

        private static async Task<List<T>> QueryAllAsync<T>(TableClient table)
            where T : class, ITableEntity, new()
        {
            var results = new List<T>();
            await foreach (var entity in table.QueryAsync<T>().ConfigureAwait(false))
            {
                results.Add(entity);
            }
            return results;
        }
    }
}
