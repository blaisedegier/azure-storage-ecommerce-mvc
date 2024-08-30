using Microsoft.AspNetCore.Mvc;
using BasicEcom.Attributes;
using BasicEcom.Models;
using BasicEcom.Services;
using BasicEcom.ViewModels;

namespace BasicEcom.Controllers
{
    public class ProductsController : Controller
    {
        // Services
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly BlobService _blobService;
        private readonly FileService _fileService;
        private readonly PdfGeneratorService _pdfGeneratorService;

        public ProductsController(IConfiguration configuration)
        {
            // Get the connection string from the configuration
            string connectionString = configuration["AzureStorage:ConnectionString"]!;

            // Get the table names from the configuration and create the TableService
            string customersTableName = configuration["AzureStorage:CustomersTableName"]!;
            string productsTableName = configuration["AzureStorage:ProductsTableName"]!;
            string ordersTableName = configuration["AzureStorage:OrdersTableName"]!;
            _tableService = new TableService(connectionString, customersTableName, productsTableName, ordersTableName);

            // Get the queue name from the configuration and create the QueueService
            string queueName = configuration["AzureStorage:QueueName"]!;
            _queueService = new QueueService(connectionString, queueName);

            // Get the blob container name from the configuration and create the BlobService
            string containerName = configuration["AzureStorage:BlobContainerName"]!;
            _blobService = new BlobService(connectionString, containerName);

            // Get the file share name from the configuration and create the FileService
            string shareName = configuration["AzureStorage:FileShareName"]!;
            _fileService = new FileService(connectionString, shareName);

            // Create the PdfGeneratorService
            _pdfGeneratorService = new PdfGeneratorService();
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            // Get the products from the table storage
            string filter = string.Empty;
            var products = await _tableService.QueryProductsAsync(filter).ConfigureAwait(false);

            // Return the view with the products sorted by name
            return View(products.OrderBy(p => p.Name));
        }

        // POST: Products
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Index(string viewType)
        {
            // Store the viewType in the session
            HttpContext.Session.SetString("viewType", viewType);

            // Get the products from the table storage
            string filter = string.Empty;
            var products = await _tableService.QueryProductsAsync(filter).ConfigureAwait(false);

            // Return the view with the products sorted by name
            return View(products.OrderBy(p => p.Name));
        }

        #region CRUD Operations
        // GET: Products/Create
        [AdminAuthorize]
        public IActionResult Create()
        {
            // Return the view
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Create([Bind("PartitionKey,RowKey,ProductID,Name,Description,Price,ImageUrl")] Products product, IFormFile imageFile)
        {
            // Check if the model is valid and return the view if not
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // Upload the image file to the blob storage
            if (imageFile != null && imageFile.Length > 0)
            {
                string blobName = product.Name!;
                using (var stream = imageFile.OpenReadStream())
                {
                    await _blobService.UploadBlobAsync(blobName, stream).ConfigureAwait(false);
                }
                product.ImageUrl = _blobService.GetBlobUrl(blobName);
            }

            // Add the product to the table storage
            product.PartitionKey = "Sneaker";
            await _tableService.AddProductAsync(product).ConfigureAwait(false);

            // Enqueue a message to the queue
            await _queueService.EnqueueMessageAsync(new { TableName = "Products", Action = "Create", Description = $"Order {product.Name} created." }).ConfigureAwait(false);

            // Redirect to the index view
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Details
        [AdminAuthorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            // Get the product from the table storage
            var product = await _tableService.GetProductAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Return NotFound if the product is null or the view with the product
            return product == null ? NotFound() : View(product);
        }

        // GET: Products/Edit
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            // Get the product from the table storage
            var product = await _tableService.GetProductAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Return NotFound if the product is null or the view with the product
            return product == null ? NotFound() : View(product);
        }

        // POST: Products/Edit
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,ProductID,Name,Description,Price,ImageUrl")] Products product)
        {
            // Check if the partitionKey and rowKey match the product and return BadRequest if not
            if (partitionKey != product.PartitionKey || rowKey != product.RowKey)
            {
                return NotFound();
            }

            // Check if the model is valid and return the view if not
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // Upload the image file to the blob storage
            await _tableService.UpdateProductAsync(product).ConfigureAwait(false);

            // Enqueue a message to the queue
            await _queueService.EnqueueMessageAsync(new { TableName = "Products", Action = "Update", Description = $"Order {product.Name} updated." }).ConfigureAwait(false);

            // Redirect to the index view
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete
        [AdminAuthorize]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            // Get the product from the table storage
            var product = await _tableService.GetProductAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Return NotFound if the product is null or the view with the product
            return product == null ? NotFound() : View(product);
        }

        // POST: Products/Delete
        [HttpPost, ActionName("DeleteConfirmed")]
        [AdminAuthorize]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            // Get the product from the table storage
            var product = await _tableService.GetProductAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Delete the product from the table storage
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                var blobName = new Uri(product.ImageUrl).Segments.Last();
                await _blobService.DeleteBlobAsync(blobName).ConfigureAwait(false);
            }

            // Enqueue a message to the queue
            await _queueService.EnqueueMessageAsync(new { TableName = "Products", Action = "Delete", Description = $"Order {product!.Name} deleted." }).ConfigureAwait(false);

            // Delete the product from the table storage
            await _tableService.DeleteProductAsync(partitionKey, rowKey).ConfigureAwait(false);

            // Redirect to the index view
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Order
        // GET: Products/CreateOrder
        public IActionResult CreateOrder(string PartitionKey, string RowKey)
        {
            // Check if the session token is null or empty and redirect to the login view if it is
            var sessionToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(sessionToken))
            {
                return RedirectToAction("Login", "Customers");
            }

            // Return the view with the order view model
            var orderVM = new OrderViewModel { ProductPartitionKey = PartitionKey, ProductRowKey = RowKey };
            return View(orderVM);
        }

        // POST: Products/CreateOrder
        /*
         * Code Attribution:
         * ViewModels in ASP.NET MVC applications - This is how it works
         * tutorialsEU - C#
         * 10 February 2023
         * YouTube
         * https://www.youtube.com/watch?v=NfUccG5faBQ
         */
        [HttpPost]
        public async Task<IActionResult> CreateOrder([Bind("ProductPartitionKey,ProductRowKey,Size,Quantity,Colour")] OrderViewModel orderVM)
        {
            // Check if the model is valid and return the view if not
            if (!ModelState.IsValid)
            {
                return View(orderVM);
            }

            // Get the customer partition key and row key from the session and return NotFound if they are null or empty
            var customerPartitionKey = HttpContext.Session.GetString("PartitionKey");
            var customerRowKey = HttpContext.Session.GetString("RowKey");
            if (string.IsNullOrEmpty(customerPartitionKey) || string.IsNullOrEmpty(customerRowKey))
            {
                return NotFound();
            }

            // Get the customer from the table storage and return NotFound if it is null
            var customer = await _tableService.GetCustomerAsync(customerPartitionKey, customerRowKey).ConfigureAwait(false);
            if (customer == null)
            {
                return NotFound();
            }

            // Get the product from the table storage
            var product = await _tableService.GetProductAsync(orderVM.ProductPartitionKey!, orderVM.ProductRowKey!).ConfigureAwait(false);

            // Create the order and add it to the table storage
            var order = new Orders
            {
                PartitionKey = Guid.NewGuid().ToString(),
                RowKey = Guid.NewGuid().ToString(),
                OrderID = Guid.NewGuid().ToString(),
                CustomerID = customer.CustomerID,
                ProductID = product.ProductID,
                Size = orderVM.Size,
                Quantity = orderVM.Quantity,
                Colour = orderVM.Colour
            };
            await _tableService.AddOrderAsync(order).ConfigureAwait(false);

            // Enqueue a message to the queue
            await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Create", Description = $"Order {order.OrderID} created." }).ConfigureAwait(false);

            // Create the invoice
            var invoice = new Invoice
            {
                CustomerName = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                OrderDate = DateTime.UtcNow,
                ProductName = product.Name,
                Description = product.Description,
                Price = product.Price,
                Size = order.Size,
                Quantity = order.Quantity,
                Colour = order.Colour
            };

            // Generate the invoice PDF
            byte[] pdfBytes = _pdfGeneratorService.GenerateInvoicePdf(invoice);

            string directoryName = customerPartitionKey;
            string fileName = $"{customer.Name}_{product.Name}_Invoice.pdf";
            try
            {
                // Upload the PDF file to the file share
                using (var stream = new MemoryStream(pdfBytes))
                {
                    await _fileService.UploadFileAsync(directoryName, fileName, stream).ConfigureAwait(false);
                }

                // Download the PDF file from the file share, check if it is null and return BadRequest if it is
                var fileStream = await _fileService.DownloadFileAsync(directoryName, fileName).ConfigureAwait(false);
                if (fileStream == null)
                {
                    return BadRequest();
                }

                // Return the PDF file
                return File(fileStream, "application/pdf", fileName);
            }
            catch
            {
                return BadRequest();
            }
        }
        #endregion
    }
}
