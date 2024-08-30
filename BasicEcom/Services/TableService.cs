using Azure;
using Azure.Data.Tables;
using BasicEcom.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace BasicEcom.Services
{
    // This class is responsible for interacting with the Azure Table Storage service.
    public class TableService
    {
        /*
         * Code Attribution:
         * TableServiceClient Class (Azure.Data.Tables) - Azure for .NET Developers
         * azure-sdk
         * 30 August 2024
         * learn.microsoft
         * https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableserviceclient?view=azure-dotnet
         */
        private readonly TableServiceClient _tableServiceClient;
        /*
         * Code Attribution:
         * TableClient Class (Azure.Data.Tables) - Azure for .NET Developers
         * azure-sdk
         * 30 August 2024
         * learn.microsoft
         * https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient?view=azure-dotnet
         */
        private readonly TableClient _customersTableClient;
        private readonly TableClient _productsTableClient;
        private readonly TableClient _ordersTableClient;

        public TableService(string connectionString, string customersTableName, string productsTableName, string ordersTableName)
        {
            _tableServiceClient = new TableServiceClient(connectionString);

            _customersTableClient = _tableServiceClient.GetTableClient(customersTableName);
            _customersTableClient.CreateIfNotExists();

            _productsTableClient = _tableServiceClient.GetTableClient(productsTableName);
            _productsTableClient.CreateIfNotExists();

            _ordersTableClient = _tableServiceClient.GetTableClient(ordersTableName);
            _ordersTableClient.CreateIfNotExists();
        }
        /*
         * Code Attribution:
         * .NET Core CRUD Operation Using Azure Table API
         * Sathiyamoorthy S
         * 21 February 2024
         * c-sharpcorner
         * https://www.c-sharpcorner.com/article/net-core-crud-operation-using-azure-table-api/
         */
        #region Customers
        #region CRUD Operations
        public async Task AddCustomerAsync(Customers profile)
        {
            try
            {
                await _customersTableClient.AddEntityAsync(profile).ConfigureAwait(false);
                Debug.WriteLine($"Customer {profile.Name} added successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding customer: {ex.Message}");
                throw new Exception("Error adding customer", ex);
            }
        }

        public async Task<Customers> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var customer = await _customersTableClient.GetEntityAsync<Customers>(partitionKey, rowKey).ConfigureAwait(false);
                Debug.WriteLine($"Customer {rowKey} retrieved successfully.");
                return customer;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving customer: {ex.Message}");
                throw new Exception("Error retrieving customer", ex);
            }
        }

        public async Task UpdateCustomerAsync(Customers profile)
        {
            try
            {
                await _customersTableClient.UpdateEntityAsync(profile, profile.ETag, TableUpdateMode.Replace).ConfigureAwait(false);
                Debug.WriteLine($"Customer {profile.Name} updated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating customer: {ex.Message}");
                throw new Exception("Error updating customer", ex);
            }
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _customersTableClient.DeleteEntityAsync(partitionKey, rowKey).ConfigureAwait(false);
                Debug.WriteLine($"Customer {rowKey} deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting customer: {ex.Message}");
                throw new Exception("Error deleting customer", ex);
            }
        }

        /*
         * Code Attribution:
         * TableClient.Query Method (Azure.Data.Tables) - Azure for .NET Developers
         * azure-sdk
         * 30 August 2024
         * learn.microsoft
         * https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.query?view=azure-dotnet
         */
        public async Task<List<Customers>> QueryCustomersAsync(string filter)
        {
            List<Customers> customersList = new List<Customers>();
            try
            {
                await Task.Run(() =>
                {
                    Pageable<Customers> customers = _customersTableClient.Query<Customers>(filter);
                    foreach (var customer in customers)
                    {
                        customersList.Add(customer);
                    }
                }).ConfigureAwait(false);
                Debug.WriteLine($"Queried {customersList.Count} customers.");
            }
            catch (RequestFailedException ex)
            {
                Debug.WriteLine($"Error querying customers: {ex.Message}");
                throw new Exception("Error querying customers", ex);
            }
            return customersList;
        }
        #endregion

        /*
         * Code Attribution:
         * ASP.NET Core Data Protection Overview
         * tdykstra
         * 30 August 2024
         * learn.microsoft
         * https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction?view=aspnetcore-8.0
         */
        #region Authentication
        public async Task<Customers?> AuthenticateCustomerAsync(string email, string password)
        {
            try
            {
                string escapedEmail = email.Replace("'", "''");
                string filter = $"Email eq '{escapedEmail}'";

                var customers = _customersTableClient.Query<Customers>(filter);
                var customer = customers.FirstOrDefault();

                if (customer != null && VerifyPasswordHash(password, customer.PasswordHash!))
                {
                    customer.SessionToken = GenerateSessionToken();
                    customer.LastLogin = DateTimeOffset.UtcNow;
                    await UpdateCustomerAsync(customer).ConfigureAwait(false);
                    return customer;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error authenticating customer: {ex.Message}");
                throw new Exception("Error authenticating customer", ex);
            }

            return null;
        }

        public async Task LogoutCustomerAsync(string partitionKey, string rowKey)
        {
            var customer = await GetCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);
            if (customer != null)
            {
                customer.SessionToken = null;
                await UpdateCustomerAsync(customer).ConfigureAwait(false);
            }
        }
        #endregion

        #region Password Hashing
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }

        private string GenerateSessionToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
        #endregion
        #endregion

        #region Products
        public async Task AddProductAsync(Products product)
        {
            try
            {
                await _productsTableClient.AddEntityAsync(product).ConfigureAwait(false);
                Debug.WriteLine($"Product {product.Name} added successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding product: {ex.Message}");
                throw new Exception("Error adding product", ex);
            }
        }

        public async Task<Products> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var product = await _productsTableClient.GetEntityAsync<Products>(partitionKey, rowKey).ConfigureAwait(false);
                Debug.WriteLine($"Product {rowKey} retrieved successfully.");
                return product;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving product: {ex.Message}");
                throw new Exception("Error retrieving product", ex);
            }
        }

        public async Task UpdateProductAsync(Products product)
        {
            try
            {
                await _productsTableClient.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace).ConfigureAwait(false);
                Debug.WriteLine($"Product {product.Name} updated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating product: {ex.Message}");
                throw new Exception("Error updating product", ex);
            }
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _productsTableClient.DeleteEntityAsync(partitionKey, rowKey).ConfigureAwait(false);
                Debug.WriteLine($"Product {rowKey} deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting product: {ex.Message}");
                throw new Exception("Error deleting product", ex);
            }
        }

        public async Task<List<Products>> QueryProductsAsync(string filter)
        {
            List<Products> productsList = new List<Products>();
            try
            {
                await Task.Run(() =>
                {
                    Pageable<Products> products = _productsTableClient.Query<Products>(filter);
                    foreach (var product in products)
                    {
                        productsList.Add(product);
                    }
                }).ConfigureAwait(false);
                Debug.WriteLine($"Queried {productsList.Count} products.");
            }
            catch (RequestFailedException ex)
            {
                Debug.WriteLine($"Error querying products: {ex.Message}");
                throw new Exception("Error querying products", ex);
            }
            return productsList;
        }
        #endregion

        #region Orders
        public async Task AddOrderAsync(Orders order)
        {
            try
            {
                await _ordersTableClient.AddEntityAsync(order).ConfigureAwait(false);
                Debug.WriteLine($"Order {order.RowKey} added successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding order: {ex.Message}");
                throw new Exception("Error adding order", ex);
            }
        }

        public async Task<Orders> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var order = await _ordersTableClient.GetEntityAsync<Orders>(partitionKey, rowKey).ConfigureAwait(false);
                Debug.WriteLine($"Order {rowKey} retrieved successfully.");
                return order;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving order: {ex.Message}");
                throw new Exception("Error retrieving order", ex);
            }
        }

        public async Task UpdateOrderAsync(Orders order)
        {
            try
            {
                await _ordersTableClient.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace).ConfigureAwait(false);
                Debug.WriteLine($"Order {order.RowKey} updated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating order: {ex.Message}");
                throw new Exception("Error updating order", ex);
            }
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _ordersTableClient.DeleteEntityAsync(partitionKey, rowKey).ConfigureAwait(false);
                Debug.WriteLine($"Order {rowKey} deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting order: {ex.Message}");
                throw new Exception("Error deleting order", ex);
            }
        }

        public async Task<List<Orders>> QueryOrdersAsync(string filter)
        {
            List<Orders> ordersList = new List<Orders>();
            try
            {
                await Task.Run(() =>
                {
                    Pageable<Orders> orders = _ordersTableClient.Query<Orders>(filter);
                    foreach (var order in orders)
                    {
                        ordersList.Add(order);
                    }
                }).ConfigureAwait(false);
                Debug.WriteLine($"Queried {ordersList.Count} orders.");
            }
            catch (RequestFailedException ex)
            {
                Debug.WriteLine($"Error querying orders: {ex.Message}");
                throw new Exception("Error querying orders", ex);
            }
            return ordersList;
        }
        #endregion
    }
}
