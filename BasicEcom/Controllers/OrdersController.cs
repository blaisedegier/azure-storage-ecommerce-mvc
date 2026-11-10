using Microsoft.AspNetCore.Mvc;
using BasicEcom.Attributes;
using BasicEcom.Models;
using BasicEcom.Services;

namespace BasicEcom.Controllers
{
    // Order administration. Customers place orders through ProductsController.CreateOrder.
    [AdminAuthorize]
    public class OrdersController : Controller
    {
        private readonly TableService _tableService;
        private readonly QueueService _queueService;

        public OrdersController(TableService tableService, QueueService queueService)
        {
            _tableService = tableService;
            _queueService = queueService;
        }

        // GET: Orders
        public async Task<ActionResult> Index()
        {
            var orders = await _tableService.QueryOrdersAsync();
            return View(orders.OrderBy(o => o.Timestamp));
        }

        #region CRUD Operations
        // GET: Orders/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        public async Task<ActionResult> Create(Orders order)
        {
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            order.PartitionKey = "Orders";
            order.RowKey = Guid.NewGuid().ToString();
            order.OrderID ??= Guid.NewGuid().ToString();

            await _tableService.AddOrderAsync(order);
            await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Create", Description = $"Order {order.OrderID} created." });

            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Details
        public async Task<ActionResult> Details(string partitionKey, string rowKey)
        {
            var order = await _tableService.FindOrderAsync(partitionKey, rowKey);
            return order == null ? NotFound() : View(order);
        }

        // GET: Orders/Edit
        public async Task<ActionResult> Edit(string partitionKey, string rowKey)
        {
            var order = await _tableService.FindOrderAsync(partitionKey, rowKey);
            return order == null ? NotFound() : View(order);
        }

        // POST: Orders/Edit
        [HttpPost]
        public async Task<ActionResult> Edit(string partitionKey, string rowKey, Orders updatedOrder)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedOrder);
            }

            var order = await _tableService.FindOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            order.CustomerID = updatedOrder.CustomerID;
            order.ProductID = updatedOrder.ProductID;
            order.Size = updatedOrder.Size;
            order.Quantity = updatedOrder.Quantity;
            order.Colour = updatedOrder.Colour;

            await _tableService.UpdateOrderAsync(order);
            await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Update", Description = $"Order {order.OrderID} updated." });

            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Delete
        public async Task<ActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _tableService.FindOrderAsync(partitionKey, rowKey);
            return order == null ? NotFound() : View(order);
        }

        // POST: Orders/Delete
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var order = await _tableService.FindOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            await _tableService.DeleteOrderAsync(partitionKey, rowKey);
            await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Delete", Description = $"Order {order.OrderID} deleted." });

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
