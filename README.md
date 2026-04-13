# Sneaker Store - Azure-Powered E-Commerce Web App 🛒

A server-rendered e-commerce application built with **ASP.NET Core 8 MVC** and **Azure Storage** services. The app demonstrates cloud storage patterns by using Azure Table, Blob, Queue, and File Share storage as its sole persistence layer- no traditional relational database required.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Azure Storage Resources](#azure-storage-resources)
- [Authentication & Authorization](#authentication--authorization)
- [Routes](#routes)
- [Data Models](#data-models)
- [Services](#services)
- [Known Limitations](#known-limitations)

---

## Features

- **Product Catalog:** Browse sneaker products with images stored in Azure Blob Storage
- **Customer Registration & Login:** Session-based authentication with SHA-256 password hashing
- **Admin Dashboard:** Full CRUD management for customers, products, and orders (admin-only)
- **Order Checkout:** Place orders, generate PDF invoices (via QuestPDF), and download them instantly
- **Azure Queue Logging:** Key user actions (login, logout, register, CRUD operations) are published as JSON messages to Azure Queue Storage
- **Invoice File Storage:** Generated PDF invoices are persisted to Azure File Shares, organized per customer
- **Role-Based Access:** Admin-only routes protected by a custom `AdminAuthorize` action filter

---

## Tech Stack

| Layer              | Technology                                                  |
| ------------------ | ----------------------------------------------------------- |
| **Framework**      | ASP.NET Core 8 MVC (.NET 8)                                 |
| **Language**       | C#                                                          |
| **UI**             | Razor Views, Bootstrap 5, jQuery + Validation               |
| **Table Storage**  | Azure Data Tables (`Azure.Data.Tables`)                     |
| **Blob Storage**   | Azure Blob Storage (`Azure.Storage.Blobs`) — product images |
| **Queue Storage**  | Azure Queue Storage (`Azure.Storage.Queues`) — event log    |
| **File Storage**   | Azure File Shares (`Azure.Storage.Files.Shares`) — invoices |
| **PDF Generation** | QuestPDF (Community License)                                |
| **Session**        | In-memory distributed cache + ASP.NET Core Session          |

---

## Architecture Overview

```text
┌──────────────┐       ┌──────────────────────────────────────────┐
│   Browser    │◄─────►│        ASP.NET Core 8 MVC App            │
│  (Razor UI)  │       │                                          │
└──────────────┘       │  Controllers ─► Services ─► Azure SDK    │
                       │                                          │
                       └────────┬─────────┬──────────┬────────────┘
                                │         │          │
                       ┌────────▼──┐ ┌────▼────┐ ┌───▼─────┐
                       │Azure Table│ │Azure    │ │Azure    │
                       │ Storage   │ │Blob     │ │Queue    │
                       │(Customers,│ │(Product │ │(Action  │
                       │ Products, │ │ Images) │ │ Events) │
                       │ Orders)   │ │         │ │         │
                       └───────────┘ └─────────┘ └─────────┘
                                                      │
                                              ┌───────▼────────┐
                                              │  Azure File    │
                                              │  Share         │
                                              │  (Invoice PDFs)│
                                              └────────────────┘
```

---

## Project Structure

```text
BasicEcom/
├── BasicEcom.sln        # Visual Studio solution
├── .gitignore
├── .gitattributes
└── BasicEcom/           # Main web project
    ├── Program.cs                        # App startup, DI, middleware
    ├── appsettings.json                  # Azure Storage configuration
    ├── BasicEcom.csproj  # .NET 8 project file
    ├── libman.json                       # Client-side library manager
    │
    ├── Controllers/
    │   ├── HomeController.cs             # Home & About pages
    │   ├── ProductsController.cs         # Product CRUD + checkout flow
    │   ├── CustomersController.cs        # Registration, login, admin CRUD
    │   └── OrdersController.cs           # Order management
    │
    ├── Models/
    │   ├── Customers.cs                  # Azure Table entity
    │   ├── Products.cs                   # Azure Table entity
    │   ├── Orders.cs                     # Azure Table entity
    │   └── Invoice.cs                    # DTO for PDF generation
    │
    ├── ViewModels/
    │   ├── RegisterViewModel.cs
    │   ├── LoginViewModel.cs
    │   ├── OrderViewModel.cs
    │   └── ErrorViewModel.cs
    │
    ├── Services/
    │   ├── TableService.cs               # Azure Table CRUD operations
    │   ├── BlobService.cs                # Image upload/delete to Blob
    │   ├── QueueService.cs               # Message publishing to Queue
    │   ├── FileService.cs                # File Share upload/download
    │   └── PdfGeneratorService.cs        # QuestPDF invoice generation
    │
    ├── Attributes/
    │   ├── AuthorizeAdminAttribute.cs    # Admin-only action filter
    │   └── AllowedExtensionsAttribute.cs # File upload validation
    │
    ├── Views/                            # Razor views
    │   ├── Home/
    │   ├── Products/
    │   ├── Customers/
    │   ├── Orders/
    │   └── Shared/
    │       └── _Layout.cshtml            # Main layout template
    │
    ├── wwwroot/                          # Static assets
    │   ├── css/
    │   ├── js/
    │   └── lib/                          # Bootstrap, jQuery
    │
    └── Properties/
        └── launchSettings.json           # Dev server URLs
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An **Azure Storage Account** with the following resources provisioned (see [Azure Storage Resources](#azure-storage-resources)), **or** [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) for local emulation
- Visual Studio 2022+ (recommended) or any editor with .NET CLI support

---

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd BasicEcom
```

### 2. Configure Azure Storage

Copy or edit `appsettings.json` (or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)) to provide your Azure Storage connection string:

```json
{
  "AzureStorage": {
    "ConnectionString": "<your-azure-storage-connection-string>",
    "BlobContainerName": "images",
    "CustomersTableName": "customers",
    "ProductsTableName": "products",
    "OrdersTableName": "orders",
    "QueueName": "actions",
    "FileShareName": "contracts",
    "ShareName": "contracts"
  }
}
```

> **Note:** The app reads `AzureStorage:ShareName` in `Program.cs` for the `FileService` DI registration. Make sure this key is present alongside `FileShareName`.

### 3. Restore and run

```bash
dotnet restore
dotnet run --project BasicEcom
```

### 4. Open in browser

| Profile     | URL                      |
| ----------- | ------------------------ |
| HTTP        | <http://localhost:5126>  |
| HTTPS       | <https://localhost:7045> |
| IIS Express | <http://localhost:37341> |

---

## Configuration

All configuration lives in `appsettings.json` under the `AzureStorage` section:

| Key                  | Description                             | Default Value |
| -------------------- | --------------------------------------- | ------------- |
| `ConnectionString`   | Azure Storage account connection string | _(required)_  |
| `BlobContainerName`  | Blob container for product images       | `images`      |
| `CustomersTableName` | Table name for customer entities        | `customers`   |
| `ProductsTableName`  | Table name for product entities         | `products`    |
| `OrdersTableName`    | Table name for order entities           | `orders`      |
| `QueueName`          | Queue name for action event messages    | `actions`     |
| `FileShareName`      | File share name for invoice PDFs        | `contracts`   |
| `ShareName`          | File share name used by DI registration | `contracts`   |

For local development, consider using **Azurite** and pointing the connection string to `UseDevelopmentStorage=true`.

---

## Azure Storage Resources

The application expects these Azure Storage resources to exist (they are auto-created on startup where supported):

| Resource Type  | Name        | Purpose                              |
| -------------- | ----------- | ------------------------------------ |
| **Table**      | `customers` | Customer accounts and admin flags    |
| **Table**      | `products`  | Product catalog (sneakers)           |
| **Table**      | `orders`    | Customer orders                      |
| **Blob**       | `images`    | Product image uploads                |
| **Queue**      | `actions`   | JSON event messages for user actions |
| **File Share** | `contracts` | Generated PDF invoices per customer  |

---

## Authentication & Authorization

The app uses a **custom session-based authentication** system (not ASP.NET Core Identity):

- **Password security:** Passwords are hashed using **SHA-256** before storage
- **Session management:** A unique `SessionToken` is generated on login and stored in both the session cookie and the Azure Table entity; session timeout is 30 minutes
- **Admin role:** Controlled by the `isAdmin` boolean field on the `Customers` table entity
- **Route protection:** The `AdminAuthorizeAttribute` action filter checks the session for admin status by loading the customer entity from Azure Table Storage
- **UI guards:** Navigation links for Customers and Orders are conditionally rendered based on admin session state

### User flows

| Flow             | Access     | Description                               |
| ---------------- | ---------- | ----------------------------------------- |
| Register         | Public     | Create a new customer account             |
| Login / Logout   | Public     | Authenticate via email & password         |
| Browse Products  | Public     | View the product catalog                  |
| Place Order      | Logged in  | Checkout a product, receive a PDF invoice |
| Manage Products  | Admin only | Create, edit, delete products             |
| Manage Customers | Admin only | Full CRUD on customer records             |
| Manage Orders    | Admin only | View, edit, delete orders                 |

---

## Routes

The app uses ASP.NET Core conventional routing (`{controller=Home}/{action=Index}/{id?}`):

| Route                        | Method   | Description                        |
| ---------------------------- | -------- | ---------------------------------- |
| `/`                          | GET      | Home page                          |
| `/Home/About`                | GET      | About page                         |
| `/Products`                  | GET      | Product listing                    |
| `/Products/Create`           | GET/POST | Add new product (admin)            |
| `/Products/Edit/{id}`        | GET/POST | Edit product (admin)               |
| `/Products/Delete/{id}`      | GET/POST | Delete product (admin)             |
| `/Products/Details/{id}`     | GET      | Product detail view                |
| `/Products/CreateOrder/{id}` | GET/POST | Checkout — creates order + invoice |
| `/Customers/Register`        | GET/POST | Customer registration              |
| `/Customers/Login`           | GET/POST | Customer login                     |
| `/Customers/Logout`          | POST     | Customer logout                    |
| `/Customers`                 | GET      | Customer listing (admin)           |
| `/Customers/Create`          | GET/POST | Add customer (admin)               |
| `/Customers/Edit/{id}`       | GET/POST | Edit customer (admin)              |
| `/Customers/Delete/{id}`     | GET/POST | Delete customer (admin)            |
| `/Orders`                    | GET      | Order listing (admin)              |
| `/Orders/Details/{id}`       | GET      | Order details                      |
| `/Orders/Edit/{id}`          | GET/POST | Edit order (admin)                 |
| `/Orders/Delete/{id}`        | GET/POST | Delete order (admin)               |

---

## Data Models

### Customers (Azure Table Entity)

| Field          | Type     | Description                             |
| -------------- | -------- | --------------------------------------- |
| `PartitionKey` | string   | `"Customer"`                            |
| `RowKey`       | string   | Auto-generated GUID                     |
| `CustomerID`   | string   | Derived from name prefix + phone suffix |
| `Name`         | string   | Full name                               |
| `Email`        | string   | Login email                             |
| `PasswordHash` | string   | SHA-256 hashed password                 |
| `Phone`        | string   | Phone number                            |
| `Address`      | string   | Mailing address                         |
| `isAdmin`      | bool     | Admin role flag                         |
| `LastLogin`    | DateTime | Timestamp of last login                 |
| `SessionToken` | string   | Active session token                    |

### Products (Azure Table Entity)

| Field          | Type    | Description                    |
| -------------- | ------- | ------------------------------ |
| `PartitionKey` | string  | `"Sneaker"`                    |
| `RowKey`       | string  | Auto-generated GUID            |
| `ProductID`    | string  | Product identifier             |
| `Name`         | string  | Product name                   |
| `Description`  | string  | Product description            |
| `Price`        | decimal | Price                          |
| `ImageUrl`     | string  | Blob URL for the product image |

### Orders (Azure Table Entity)

| Field          | Type   | Description      |
| -------------- | ------ | ---------------- |
| `PartitionKey` | string | Partition key    |
| `RowKey`       | string | Auto-generated   |
| `OrderID`      | string | Order identifier |
| `CustomerID`   | string | Linked customer  |
| `ProductID`    | string | Linked product   |
| `Size`         | string | Selected size    |
| `Quantity`     | int    | Order quantity   |
| `Colour`       | string | Selected colour  |

### Invoice (DTO)

| Field          | Type    | Description                       |
| -------------- | ------- | --------------------------------- |
| _Order fields_ | —       | Inherits order data               |
| `TotalAmount`  | decimal | Computed total (price × quantity) |

---

## Services

| Service               | Responsibility                                                     |
| --------------------- | ------------------------------------------------------------------ |
| `TableService`        | CRUD operations against Azure Table Storage for all three entities |
| `BlobService`         | Upload and delete product images to/from Azure Blob Storage        |
| `QueueService`        | Enqueue JSON-serialized action events to Azure Queue Storage       |
| `FileService`         | Upload and download invoice PDFs to/from Azure File Shares         |
| `PdfGeneratorService` | Generate PDF invoices using QuestPDF (Community License)           |

---

## Known Limitations

- **No automated tests:** The project does not include a test suite
- **No CI/CD pipeline:** No GitHub Actions, Azure Pipelines, or Dockerfile is configured
- **Config key mismatch:** `Program.cs` reads `AzureStorage:ShareName` for the `FileService`, while `appsettings.json` defines `FileShareName`. Both keys should be present for the app to start correctly
- **Sync-over-async:** `AdminAuthorizeAttribute` calls `.Result` on an async method, which can cause deadlocks under certain hosting conditions
- **SHA-256 hashing:** Passwords are hashed with plain SHA-256 rather than a purpose-built algorithm like bcrypt or PBKDF2
