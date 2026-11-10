using Microsoft.AspNetCore.Mvc;
using BasicEcom.Attributes;
using BasicEcom.Models;
using BasicEcom.Services;
using BasicEcom.ViewModels;

namespace BasicEcom.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly BlobService _blobService;
        private readonly FileService _fileService;
        private readonly PdfGeneratorService _pdfGeneratorService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            TableService tableService,
            QueueService queueService,
            BlobService blobService,
            FileService fileService,
            PdfGeneratorService pdfGeneratorService,
            ILogger<ProductsController> logger)
        {
            _tableService = tableService;
            _queueService = queueService;
            _blobService = blobService;
            _fileService = fileService;
            _pdfGeneratorService = pdfGeneratorService;
            _logger = logger;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _tableService.QueryProductsAsync();
            return View(products.OrderBy(p => p.Name));
        }

        // POST: Products (switches an admin between the card and table views)
        [HttpPost]
        [AdminAuthorize]
        public IActionResult Index(string viewType)
        {
            HttpContext.Session.SetString("viewType", viewType == "table" ? "table" : "card");
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Image - streams a product image from the private blob container.
        public async Task<IActionResult> Image(string partitionKey, string rowKey)
        {
            var product = await _tableService.FindProductAsync(partitionKey, rowKey);
            if (string.IsNullOrEmpty(product?.ImageUrl))
            {
                return NotFound();
            }

            var blob = await _blobService.DownloadBlobAsync(product.ImageUrl);
            return blob == null ? NotFound() : File(blob.Value.Content, blob.Value.ContentType);
        }

        #region CRUD Operations
        // GET: Products/Create
        [AdminAuthorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Create([Bind("ProductID,Name,Description,Price")] Products product,
            [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png" })] IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.PartitionKey = "Sneaker";
            product.RowKey = Guid.NewGuid().ToString();
            product.ProductID ??= product.RowKey;

            // The image is stored under a generated name; ImageUrl holds that blob name.
            if (imageFile != null && imageFile.Length > 0)
            {
                string blobName = $"{product.RowKey}{Path.GetExtension(imageFile.FileName).ToLowerInvariant()}";
                using (var stream = imageFile.OpenReadStream())
                {
                    await _blobService.UploadBlobAsync(blobName, stream, imageFile.ContentType);
                }
                product.ImageUrl = blobName;
            }

            await _tableService.AddProductAsync(product);
            await _queueService.EnqueueMessageAsync(new { TableName = "Products", Action = "Create", Description = $"Product {product.Name} created." });

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Details
        [AdminAuthorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _tableService.FindProductAsync(partitionKey, rowKey);
            return product == null ? NotFound() : View(product);
        }

        // GET: Products/Edit
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var product = await _tableService.FindProductAsync(partitionKey, rowKey);
            return product == null ? NotFound() : View(product);
        }

        // POST: Products/Edit
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,Name,Description,Price")] Products input)
        {
            if (partitionKey != input.PartitionKey || rowKey != input.RowKey)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var product = await _tableService.FindProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            product.Name = input.Name;
            product.Description = input.Description;
            product.Price = input.Price;

            await _tableService.UpdateProductAsync(product);
            await _queueService.EnqueueMessageAsync(new { TableName = "Products", Action = "Update", Description = $"Product {product.Name} updated." });

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete
        [AdminAuthorize]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _tableService.FindProductAsync(partitionKey, rowKey);
            return product == null ? NotFound() : View(product);
        }

        // POST: Products/DeleteConfirmed
        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var product = await _tableService.FindProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                await _blobService.DeleteBlobAsync(product.ImageUrl);
            }

            await _tableService.DeleteProductAsync(partitionKey, rowKey);
            await _queueService.EnqueueMessageAsync(new { TableName = "Products", Action = "Delete", Description = $"Product {product.Name} deleted." });

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Order
        // GET: Products/CreateOrder
        public IActionResult CreateOrder(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("SessionToken")))
            {
                return RedirectToAction("Login", "Customers");
            }

            return View(new OrderViewModel { ProductPartitionKey = partitionKey, ProductRowKey = rowKey });
        }

        // POST: Products/CreateOrder - stores the order and returns its invoice as a PDF download.
        // Adapted from https://www.youtube.com/watch?v=NfUccG5faBQ
        [HttpPost]
        public async Task<IActionResult> CreateOrder([Bind("ProductPartitionKey,ProductRowKey,Size,Quantity,Colour")] OrderViewModel orderVM)
        {
            var customerPartitionKey = HttpContext.Session.GetString("PartitionKey");
            var customerRowKey = HttpContext.Session.GetString("RowKey");
            if (string.IsNullOrEmpty(customerPartitionKey) || string.IsNullOrEmpty(customerRowKey))
            {
                return RedirectToAction("Login", "Customers");
            }

            if (!ModelState.IsValid)
            {
                return View(orderVM);
            }

            var customer = await _tableService.FindCustomerAsync(customerPartitionKey, customerRowKey);
            var product = await _tableService.FindProductAsync(orderVM.ProductPartitionKey ?? "", orderVM.ProductRowKey ?? "");
            if (customer == null || product == null)
            {
                return NotFound();
            }

            var order = new Orders
            {
                PartitionKey = "Orders",
                RowKey = Guid.NewGuid().ToString(),
                OrderID = Guid.NewGuid().ToString(),
                CustomerID = customer.CustomerID,
                ProductID = product.ProductID,
                Size = orderVM.Size,
                Quantity = orderVM.Quantity,
                Colour = orderVM.Colour
            };
            await _tableService.AddOrderAsync(order);
            await _queueService.EnqueueMessageAsync(new { TableName = "Orders", Action = "Create", Description = $"Order {order.OrderID} created." });

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
            byte[] pdfBytes = _pdfGeneratorService.GenerateInvoicePdf(invoice);
            string fileName = $"invoice-{order.OrderID}.pdf";

            // Keep a copy in the customer's directory on the file share. The download does not depend on it.
            try
            {
                using var stream = new MemoryStream(pdfBytes);
                await _fileService.UploadFileAsync(customer.RowKey!, fileName, stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Archiving invoice {FileName} failed.", fileName);
            }

            return File(pdfBytes, "application/pdf", fileName);
        }
        #endregion
    }
}
