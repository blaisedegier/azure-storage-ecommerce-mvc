using BasicEcom.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register TableService with Dependency Injection
builder.Services.AddSingleton<TableService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    string connectionString = configuration["AzureStorage:ConnectionString"]!;
    string customersTableName = configuration["AzureStorage:CustomersTableName"]!;
    string productsTableName = configuration["AzureStorage:ProductsTableName"]!;
    string ordersTableName = configuration["AzureStorage:OrdersTableName"]!;

    return new TableService(connectionString, customersTableName, productsTableName, ordersTableName);
});

// Register QueueService with Dependency Injection
builder.Services.AddSingleton<QueueService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    string connectionString = configuration["AzureStorage:ConnectionString"] ?? throw new ArgumentNullException(nameof(configuration), "AzureStorage:ConnectionString is not configured.");
    string queueName = configuration["AzureStorage:QueueName"] ?? throw new ArgumentNullException(nameof(configuration), "AzureStorage:QueueName is not configured.");

    return new QueueService(connectionString, queueName);
});

// Register BlobService with Dependency Injection
builder.Services.AddSingleton<BlobService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    string connectionString = configuration["AzureStorage:ConnectionString"] ?? throw new ArgumentNullException(nameof(configuration), "AzureStorage:ConnectionString is not configured.");
    string blobContainerName = configuration["AzureStorage:BlobContainerName"] ?? throw new ArgumentNullException(nameof(configuration), "AzureStorage:BlobContainerName is not configured.");

    return new BlobService(connectionString, blobContainerName);
});

// Register FileService with Dependency Injection
builder.Services.AddSingleton<FileService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    string connectionString = configuration["AzureStorage:ConnectionString"] ?? throw new ArgumentNullException(nameof(configuration), "AzureStorage:ConnectionString is not configured.");
    string shareName = configuration["AzureStorage:ShareName"] ?? throw new ArgumentNullException(nameof(configuration), "AzureStorage:ShareName is not configured.");

    return new FileService(connectionString, shareName);
});

// Register PdfGeneratorService with Dependency Injection
builder.Services.AddSingleton<PdfGeneratorService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session management
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
