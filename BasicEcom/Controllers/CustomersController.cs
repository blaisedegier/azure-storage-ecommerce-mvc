using Microsoft.AspNetCore.Mvc;
using BasicEcom.Attributes;
using BasicEcom.Models;
using BasicEcom.Services;
using BasicEcom.ViewModels;
using System.Text;

namespace BasicEcom.Controllers
{
    public class CustomersController : Controller
    {
        // Services
        private readonly TableService _tableService;
        private readonly FileService _fileService;
        private readonly QueueService _queueService;

        public CustomersController(IConfiguration configuration)
        {
            // Get the connection string from the configuration
            string connectionString = configuration["AzureStorage:ConnectionString"]!;

            // Get the table names from the configuration and create the TableService
            string customersTableName = configuration["AzureStorage:CustomersTableName"]!;
            string productsTableName = configuration["AzureStorage:ProductsTableName"]!;
            string ordersTableName = configuration["AzureStorage:OrdersTableName"]!;
            _tableService = new TableService(connectionString, customersTableName, productsTableName, ordersTableName);

            // Get the file share name from the configuration and create the FileService
            string shareName = configuration["AzureStorage:FileShareName"]!;
            _fileService = new FileService(connectionString, shareName);

            // Get the queue name from the configuration and create the QueueService
            string queueName = configuration["AzureStorage:QueueName"]!;
            _queueService = new QueueService(connectionString, queueName);
        }

        // GET: Customers
        [AdminAuthorize]
        public async Task<IActionResult> Index()
        {
            // Get the customers from the table storage
            string filter = string.Empty;
            var customers = await _tableService.QueryCustomersAsync(filter).ConfigureAwait(false);

            // Return the customers sorted by name
            return View(customers.OrderBy(c => c.Name));
        }

        #region CRUD Operations
        // GET: Customers/Create
        [AdminAuthorize]
        public IActionResult Create()
        {
            // Return the create view
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address,Password,ConfirmPassword")] RegisterViewModel model)
        {
            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create the customer
            var customer = new Customers
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                CustomerID = model.Name!.Substring(0, 3).ToUpper() + model.Phone!.Substring(model.Phone.Length - 3),
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                PasswordHash = TableService.HashPassword(model.Password!)
            };

            try
            {
                // Add the customer to the table storage
                await _tableService.AddCustomerAsync(customer).ConfigureAwait(false);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { TableName = "Customers", Action = "Create", Description = $"Customer {customer.Name} created." }).ConfigureAwait(false);

                // Redirect to the index view
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Customers/Details
        [AdminAuthorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            // Check if the partition key and row key are valid
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            // Get the customer from the table storage
            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Return the customer or not found
            return customer == null ? NotFound() : View(customer);
        }

        // GET: Customers/Edit
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            // Check if the partition key and row key are valid
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            // Get the customer from the table storage
            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Return the customer or not found
            return customer == null ? NotFound() : View(customer);
        }

        // POST: Customers/Edit
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,Name,Email,Phone,Address,PasswordHash,ETag,isAdmin")] Customers customer)
        {
            // Check if the partition key and row key are valid
            if (partitionKey != customer.PartitionKey || rowKey != customer.RowKey)
            {
                return NotFound();
            }

            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                // Update the customer in the table storage
                await _tableService.UpdateCustomerAsync(customer).ConfigureAwait(false);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { TableName = "Customers", Action = "Update", Description = $"Customer {customer.Name} updated." }).ConfigureAwait(false);

                // Redirect to the index view
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Customers/Delete
        [AdminAuthorize]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            // Check if the partition key and row key are valid
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            // Get the customer from the table storage
            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Return the customer or not found
            return customer == null ? NotFound() : View(customer);
        }

        // POST: Customers/Delete
        [HttpPost, ActionName("Delete")]
        [AdminAuthorize]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                // Get the customer from the table storage
                var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

                // Delete the customers directory from the file share
                await _fileService.DeleteFileAsync(customer.PartitionKey!);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { TableName = "Customers", Action = "Delete", Description = $"Customer {customer.Name} deleted." }).ConfigureAwait(false);
                
                // Delete the customer from the table storage
                await _tableService.DeleteCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

                // Redirect to the index view
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return NotFound();
            }
        }
        #endregion

        #region Authentication
        // GET: Customers/Login
        public IActionResult Login()
        {
            // Return the login view
            return View();
        }

        // POST: Customers/Login
        [HttpPost]
        public async Task<IActionResult> Login([Bind("Email,Password")] LoginViewModel model)
        {
            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            // Authenticate the customer
            var customer = await _tableService.AuthenticateCustomerAsync(model.Email!, model.Password!).ConfigureAwait(false);
            if (customer != null)
            {
                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { Action = "Login", Description = $"Customer {customer.Name} logged in." }).ConfigureAwait(false);

                /*
                 * Code Attribution:
                 * Using Sessions and HttpContext in ASP.NET Core and MVC Core
                 * Ben Cull
                 * 23 July 2016
                 * BenCull.com
                 * https://bencull.com/blog/using-sessions-and-httpcontext-in-aspnetcore-and-mvc-core
                 */
                HttpContext.Session.SetString("SessionToken", customer.SessionToken!);
                HttpContext.Session.SetString("CustomerName", customer.Name!);
                HttpContext.Session.SetString("PartitionKey", customer.PartitionKey!);
                HttpContext.Session.SetString("RowKey", customer.RowKey!);
                HttpContext.Session.SetString("isAdmin", customer.isAdmin.ToString());
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return NotFound();
            }
        }

        // POST: Customers/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Get the customer details from the session
            var partitionKey = HttpContext.Session.GetString("PartitionKey");
            var rowKey = HttpContext.Session.GetString("RowKey");

            // Check if the partition key and row key are valid
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return RedirectToAction("Index", "Home");
            }

            // Get the customer from the table storage
            var customer = await _tableService.GetCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Enqueue a message to the queue
            await _queueService.EnqueueMessageAsync(new { Action = "Logout", Description = $"Customer {customer.Name} logged out." }).ConfigureAwait(false);

            // Logout the customer
            await _tableService.LogoutCustomerAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Clear the session
            HttpContext.Session.Clear();

            // Redirect to the home page
            return RedirectToAction("Index", "Home");
        }

        // GET: Customers/Register
        public IActionResult Register()
        {
            // Return the register view
            return View();
        }

        // POST: Customers/Register
        [HttpPost]
        public async Task<IActionResult> Register([Bind("Name,Email,Phone,Address,Password,ConfirmPassword")] RegisterViewModel model)
        {
            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create the customer
            var customer = new Customers
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                CustomerID = model.Name!.Substring(0, 3).ToUpper() + model.Phone!.Substring(model.Phone.Length - 3),
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                PasswordHash = TableService.HashPassword(model.Password!)
            };

            try
            {
                // Add the customer to the table storage
                await _tableService.AddCustomerAsync(customer).ConfigureAwait(false);

                // Authenticate the customer
                await _tableService.AuthenticateCustomerAsync(model.Email!, model.Password!).ConfigureAwait(false);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { Action = "Register", Description = $"Customer {customer.Name} registered." }).ConfigureAwait(false);

                // Redirect to the login view
                return RedirectToAction(nameof(Login));
            }
            catch
            {
                return NotFound();
            }
        }
        #endregion
    }
}
