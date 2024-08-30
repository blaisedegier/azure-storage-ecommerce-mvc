using Microsoft.AspNetCore.Mvc;
using BasicEcom.Models;
using BasicEcom.Services;

namespace BasicEcom.Controllers
{
    public class OrdersController : Controller
    {
        // Services
        private readonly TableService _tableService;
        private readonly QueueService _queueService;

        public OrdersController(TableService tableService, IConfiguration configuration)
        {
            // Dependency Injection for TableService
            _tableService = tableService;

            // Get the queue name from the configuration and create the QueueService
            string connectionString = configuration["AzureStorage:ConnectionString"]!;
            string queueName = configuration["AzureStorage:QueueName"]!;
            _queueService = new QueueService(connectionString, queueName);
        }

        // GET: Orders
        public async Task<ActionResult> Index()
        {
            // Get the orders from the orders storage
            string filter = string.Empty;
            var orders = await _tableService.QueryOrdersAsync(filter);

            // Return the view with the orders sorted by timestamp
            return View(orders.OrderBy(o => o.Timestamp));
        }

        #region CRUD Operations
        // GET: Orders/Create
        public ActionResult Create()
        {
            // Return the view
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        public async Task<ActionResult> Create(Orders order)
        {
            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                return View(order);
            }
            order.PartitionKey = "Orders";
            order.RowKey = Guid.NewGuid().ToString();

            try
            {
                // Add the order to the storage
                await _tableService.AddOrderAsync(order);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Create", Description = $"Order {order.OrderID} created." }).ConfigureAwait(false);

                // Redirect to the index
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Orders/Details
        public async Task<ActionResult> Details(string partitionKey, string rowKey)
        {
            // Get the order from the storage
            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);

            // Return the view with the order or not found
            return order == null ? NotFound() : View(order);
        }

        // GET: Orders/Edit
        public async Task<ActionResult> Edit(string partitionKey, string rowKey)
        {
            // Get the order from the storage
            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);

            // Return the view with the order or not found
            return order == null ? NotFound() : View(order);
        }

        // POST: OrdersController/Edit
        [HttpPost]
        public async Task<ActionResult> Edit(string partitionKey, string rowKey, Orders updatedOrder)
        {
            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                return View(updatedOrder);
            }

            // Get the order from the storage and check if it is null
            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            // Update the order with the new values
            order.CustomerID = updatedOrder.CustomerID;
            order.ProductID = updatedOrder.ProductID;
            order.Size = updatedOrder.Size;
            order.Quantity = updatedOrder.Quantity;
            order.Colour = updatedOrder.Colour;

            try
            {
                // Update the order in the storage
                await _tableService.UpdateOrderAsync(order);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Update", Description = $"Order {order.OrderID} updated." }).ConfigureAwait(false);

                // Redirect to the index
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(updatedOrder);
            }
        }

        // GET: OrdersController/Delete/5
        public async Task<ActionResult> Delete(string partitionKey, string rowKey)
        {
            // Get the order from the storage
            var order = await _tableService.GetOrderAsync(partitionKey, rowKey);

            // Return the view with the order or not found
            return order == null ? NotFound() : View(order);
        }

        // POST: OrdersController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(string partitionKey, string rowKey, IFormCollection collection)
        {
            try
            {
                // Get the order from the storage
                var order = await _tableService.GetOrderAsync(partitionKey, rowKey);

                // Enqueue a message to the queue
                await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Delete", Description = $"Order {order.OrderID} deleted." }).ConfigureAwait(false);

                // Delete the order from the storage
                await _tableService.DeleteOrderAsync(partitionKey, rowKey);

                // Redirect to the index
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
        #endregion
    }
}
