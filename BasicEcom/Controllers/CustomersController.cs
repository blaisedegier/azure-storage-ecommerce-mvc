using Microsoft.AspNetCore.Mvc;
using BasicEcom.Attributes;
using BasicEcom.Models;
using BasicEcom.Services;
using BasicEcom.ViewModels;

namespace BasicEcom.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableService _tableService;
        private readonly FileService _fileService;
        private readonly QueueService _queueService;

        public CustomersController(TableService tableService, FileService fileService, QueueService queueService)
        {
            _tableService = tableService;
            _fileService = fileService;
            _queueService = queueService;
        }

        // GET: Customers
        [AdminAuthorize]
        public async Task<IActionResult> Index()
        {
            var customers = await _tableService.QueryCustomersAsync();
            return View(customers.OrderBy(c => c.Name));
        }

        #region CRUD Operations
        // GET: Customers/Create
        [AdminAuthorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address,Password,ConfirmPassword")] RegisterViewModel model)
        {
            if (!await TryAddCustomerAsync(model))
            {
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Details
        [AdminAuthorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var customer = await _tableService.FindCustomerAsync(partitionKey, rowKey);
            return customer == null ? NotFound() : View(customer);
        }

        // GET: Customers/Edit
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _tableService.FindCustomerAsync(partitionKey, rowKey);
            return customer == null ? NotFound() : View(customer);
        }

        // POST: Customers/Edit - updates contact details only; the password, admin flag and session are
        // read from the stored record, never from the form.
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,Name,Email,Phone,Address")] Customers input)
        {
            if (partitionKey != input.PartitionKey || rowKey != input.RowKey)
            {
                return NotFound();
            }

            // The form does not post the password hash, so its [Required] rule does not apply here.
            ModelState.Remove(nameof(Customers.PasswordHash));
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var customer = await _tableService.FindCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            customer.Name = input.Name;
            customer.Email = input.Email;
            customer.Phone = input.Phone;
            customer.Address = input.Address;

            await _tableService.UpdateCustomerAsync(customer);
            await _queueService.EnqueueMessageAsync(new { TableName = "Customers", Action = "Update", Description = $"Customer {customer.Name} updated." });

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Delete
        [AdminAuthorize]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _tableService.FindCustomerAsync(partitionKey, rowKey);
            return customer == null ? NotFound() : View(customer);
        }

        // POST: Customers/DeleteConfirmed - removes the customer and their archived invoices.
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var customer = await _tableService.FindCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            await _fileService.DeleteDirectoryAsync(customer.RowKey!);
            await _tableService.DeleteCustomerAsync(partitionKey, rowKey);
            await _queueService.EnqueueMessageAsync(new { TableName = "Customers", Action = "Delete", Description = $"Customer {customer.Name} deleted." });

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Authentication
        // GET: Customers/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Customers/Login
        [HttpPost]
        public async Task<IActionResult> Login([Bind("Email,Password")] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _tableService.AuthenticateCustomerAsync(model.Email!, model.Password!);
            if (customer == null)
            {
                ModelState.AddModelError(string.Empty, "Incorrect email or password.");
                return View(model);
            }

            await _queueService.EnqueueMessageAsync(new { Action = "Login", Description = $"Customer {customer.Name} logged in." });

            // Adapted from https://bencull.com/blog/using-sessions-and-httpcontext-in-aspnetcore-and-mvc-core
            HttpContext.Session.SetString("SessionToken", customer.SessionToken!);
            HttpContext.Session.SetString("CustomerName", customer.Name!);
            HttpContext.Session.SetString("PartitionKey", customer.PartitionKey!);
            HttpContext.Session.SetString("RowKey", customer.RowKey!);
            HttpContext.Session.SetString("isAdmin", customer.isAdmin.ToString());
            return RedirectToAction("Index", "Home");
        }

        // POST: Customers/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var partitionKey = HttpContext.Session.GetString("PartitionKey");
            var rowKey = HttpContext.Session.GetString("RowKey");
            if (!string.IsNullOrEmpty(partitionKey) && !string.IsNullOrEmpty(rowKey))
            {
                var customer = await _tableService.FindCustomerAsync(partitionKey, rowKey);
                if (customer != null)
                {
                    await _queueService.EnqueueMessageAsync(new { Action = "Logout", Description = $"Customer {customer.Name} logged out." });
                    await _tableService.LogoutCustomerAsync(partitionKey, rowKey);
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: Customers/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Customers/Register
        [HttpPost]
        public async Task<IActionResult> Register([Bind("Name,Email,Phone,Address,Password,ConfirmPassword")] RegisterViewModel model)
        {
            if (!await TryAddCustomerAsync(model))
            {
                return View(model);
            }

            await _queueService.EnqueueMessageAsync(new { Action = "Register", Description = $"Customer {model.Name} registered." });
            return RedirectToAction(nameof(Login));
        }
        #endregion

        // Validates the form, rejects an email that is already registered, and stores the new customer.
        private async Task<bool> TryAddCustomerAsync(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return false;
            }

            if (await _tableService.FindCustomerByEmailAsync(model.Email!) != null)
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "An account with this email already exists.");
                return false;
            }

            var customer = new Customers
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                CustomerID = model.Name!.Substring(0, 3).ToUpperInvariant() + model.Phone!.Substring(model.Phone.Length - 3),
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address
            };
            customer.PasswordHash = TableService.HashPassword(customer, model.Password!);

            await _tableService.AddCustomerAsync(customer);
            await _queueService.EnqueueMessageAsync(new { TableName = "Customers", Action = "Create", Description = $"Customer {customer.Name} created." });
            return true;
        }
    }
}
