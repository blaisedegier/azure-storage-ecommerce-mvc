using BasicEcom.Models;
using BasicEcom.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;

// QuestPDF's Community licence is free for individuals, non-profits and businesses under USD 1M annual
// revenue; see https://www.questpdf.com/license/.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Every POST must carry the antiforgery token that the form tag helper emits.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// One instance of each storage service for the app's lifetime: the SDK clients are thread-safe, and
// creating them once means the tables, container, queue and share are checked once at first use
// rather than on every request.
var storage = builder.Configuration.GetSection("AzureStorage");
string Require(string key) => storage[key]
    ?? throw new InvalidOperationException($"AzureStorage:{key} is not configured.");

builder.Services.AddSingleton(_ => new TableService(
    Require("ConnectionString"), Require("CustomersTableName"), Require("ProductsTableName"), Require("OrdersTableName")));
builder.Services.AddSingleton(_ => new QueueService(Require("ConnectionString"), Require("QueueName")));
builder.Services.AddSingleton(_ => new BlobService(Require("ConnectionString"), Require("BlobContainerName")));
builder.Services.AddSingleton(provider => new FileService(
    Require("ConnectionString"), Require("FileShareName"), provider.GetRequiredService<ILogger<FileService>>()));
builder.Services.AddSingleton<PdfGeneratorService>();

var app = builder.Build();

await SeedAdminAsync(app.Services, app.Configuration, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Creates an admin customer from the SeedAdmin settings when a password is configured and no customer
// with that email exists yet.
static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
{
    var seed = configuration.GetSection("SeedAdmin");
    var email = seed["Email"];
    var password = seed["Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogInformation("SeedAdmin:Password is not set, so no admin account was created.");
        return;
    }

    var tableService = services.GetRequiredService<TableService>();
    if (await tableService.FindCustomerByEmailAsync(email) != null)
    {
        return;
    }

    var admin = new Customers
    {
        PartitionKey = "Customer",
        RowKey = Guid.NewGuid().ToString(),
        CustomerID = "ADMIN",
        Name = seed["Name"] ?? "Store Admin",
        Email = email,
        Phone = seed["Phone"] ?? "0000000000",
        isAdmin = true
    };
    admin.PasswordHash = TableService.HashPassword(admin, password);
    await tableService.AddCustomerAsync(admin);
    logger.LogInformation("Created admin account {Email}.", email);
}
