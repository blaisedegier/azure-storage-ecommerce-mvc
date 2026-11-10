# Sneaker Store

An ASP.NET Core 8 MVC shop that keeps all of its data in Azure Storage: Table Storage for records, Blob Storage for product images, Queue Storage for an activity log and a File Share for PDF invoices.

[![CI](https://github.com/blaisedegier/azure-storage-ecommerce-mvc/actions/workflows/ci.yml/badge.svg)](https://github.com/blaisedegier/azure-storage-ecommerce-mvc/actions/workflows/ci.yml)

## What it does

- Customers register, sign in and browse the product catalogue.
- A signed-in customer orders a product by choosing a size, quantity and colour; the order is saved and a
  PDF invoice (generated with QuestPDF) downloads straight away.
- Admins add, edit and delete products (with an image upload), switch the catalogue between card and
  table views, and manage customers and orders.
- Every sign-in, sign-out, registration and create, update or delete is written to a queue as a JSON
  message.

## How it works

| Azure Storage service | Used for | Code |
| --- | --- | --- |
| Table Storage | `customers`, `products` and `orders` tables | `TableService` |
| Blob Storage | Product images in the `images` container | `BlobService` |
| Queue Storage | Activity messages in the `actions` queue | `QueueService` |
| File Share | A copy of each invoice in the `contracts` share, one directory per customer | `FileService` |

Each service wraps one SDK client and is registered as a singleton, so the tables, container, queue and
share are created if missing once, when the service is first used.

Decisions worth knowing about:

- **Sign-in is session based, not ASP.NET Core Identity.** A successful sign-in stores the customer's keys
  and a random session token in the server-side session (30 minute idle timeout). Passwords are hashed with
  Identity's `PasswordHasher<T>` (PBKDF2 with a random salt); a hash in the older unsalted SHA-256 format is
  still accepted once and replaced on that sign-in.
- **Admin pages are guarded on the server.** `[AdminAuthorize]` re-reads the customer's `isAdmin` flag from
  Table Storage on every request; the navigation links that hide admin pages are only a convenience. Every
  POST also validates an antiforgery token.
- **Images are served through the app.** Blobs are stored under generated names and streamed by
  `Products/Image`, so the container stays private and works whether or not the storage account allows
  anonymous blob access.
- **The invoice download does not depend on the file share.** The PDF is returned from memory and the copy
  on the share is best effort. Azurite emulates Blob, Queue and Table storage but not File Shares, so
  locally the app logs a warning and skips the archive.

## Running it

Prerequisites: the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and
[Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (for example
`npm install -g azurite`), or an Azure Storage account.

```bash
git clone https://github.com/blaisedegier/azure-storage-ecommerce-mvc.git
cd azure-storage-ecommerce-mvc
dotnet build BasicEcom.sln -c Release

# in a separate terminal
azurite --silent --location .azurite

dotnet user-secrets set "SeedAdmin:Password" "Choose-a-Passw0rd!" --project BasicEcom
dotnet run --project BasicEcom/BasicEcom.csproj
```

The app listens on <http://localhost:5126>. `AzureStorage:ConnectionString` in `BasicEcom/appsettings.json`
is `UseDevelopmentStorage=true`, which points at Azurite. With `SeedAdmin:Password` set, the first start
creates an admin customer, `admin@example.com` (see the `SeedAdmin` section to change it); without it no
admin exists and the admin pages cannot be reached.

To use a real storage account, set `AzureStorage:ConnectionString` to the account's connection string with
`dotnet user-secrets set` or the `AzureStorage__ConnectionString` environment variable rather than editing
`appsettings.json`. The table, container, queue and share names are also under `AzureStorage`.

QuestPDF is used under its [Community licence](https://www.questpdf.com/license/), which is free for
individuals, non-profits and businesses with less than USD 1 million in annual gross revenue; others need a
paid licence.

## Tech

ASP.NET Core 8 MVC, Azure.Data.Tables, Azure.Storage.Blobs, Azure.Storage.Queues,
Azure.Storage.Files.Shares, QuestPDF, Bootstrap 5.

## Known limitations

- Sessions are held in memory, so a restart signs everyone out and the app cannot run as more than one
  instance without a shared session store.
- Invoices are not archived when running against Azurite, which has no File Share support.
- The queue is write-only: nothing in this repository reads the activity messages.
- Listing pages read whole tables with no paging.
- Prices are stored as `double`, and the admin order form takes customer and product IDs as typed text.
- There are no automated tests.
