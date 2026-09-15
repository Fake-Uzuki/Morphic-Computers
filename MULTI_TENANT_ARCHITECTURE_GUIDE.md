# Complete Architecture & Implementation Guide: Multi-Tenant C# ERP

**Project:** Morphic Computers / IT13 ERP System  
**Architecture:** Multi-Tenant Database-per-Tenant Pattern  
**Technology Stack:** C# .NET 10, Entity Framework Core, ASP.NET Core Minimal APIs, MonsterASP Cloud SQL Server  

---

## 1. Executive Concept: What is Multi-Tenancy?

If you are new to this architecture, imagine an apartment building:
* In a single-tenant system, every customer builds their own separate house.
* In a **Multi-Tenant System (Database-per-Tenant)**, there is one central manager's office (**The Master Database**) and separate locked apartments for each client (**The Tenant Databases**).

```
                      ┌───────────────────────────────────────────────┐
                      │             ASP.NET Core Web API              │
                      │                 (ERP.api)                     │
                      └───────┬───────────────────────────────┬───────┘
                              │                               │
                              ▼                               ▼
               ┌──────────────────────────────┐ ┌──────────────────────────────┐
               │      Master Database         │ │     Dynamic Tenant Factory   │
               │   (MasterErpDbContext)       │ │   (ITenantDbContextFactory)  │
               │                              │ └─────────────┬────────────────┘
               │  - Companies                 │               │
               │  - CompanyDatabases (Routes) │               │ Look up database
               │  - Devices                   │               │ & credentials
               │  - Identity / Users          │               │
               └──────────────────────────────┘               ▼
                                           ┌──────────────────────────────────────┐
                                           │  Tenant Databases (TenantErpDbContext)│
                                           ├──────────────────┬───────────────────┤
                                           │ Tenant A (Micro) │ Tenant B (Small)  │
                                           │ db66559          │ db66562           │
                                           │  - Products      │  - Products       │
                                           │  - Customers     │  - Customers      │
                                           │  - Suppliers     │  - Suppliers      │
                                           │  - Inventories   │  - Inventories    │
                                           └──────────────────┴───────────────────┘
```

### Why Do We Use This?
1. **Total Data Isolation:** Company 1 (Micro) can never see, modify, or accidentally corrupt Company 2's data because they live in physically separate databases.
2. **Scalability:** When a new store registers, we just create a new database on MonsterASP and add one row in the Master Database.

---

## 2. Layer 1: Domain Entities (`ERP.domain/entities`)

The **Domain layer** contains plain C# classes (POCOs) that define what data exists in the system. It is divided into two categories:

### A. Master Entities (Stored in Master DB)
These entities track who uses the software.
1. **[`Company.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/Company.cs):**
   * Fields: `CompanyId`, `CompanyCode`, `CompanyName`, `IsActive`, `CreatedAt`, and a collection of `Devices`.
   * Purpose: Identifies each business entity using the software.
2. **[`CompanyDatabase.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/CompanyDatabase.cs):**
   * Fields: `CompanyDatabaseId`, `CompanyId`, `ServerName`, `DatabaseName`, `CredentialKey`, `IsActive`.
   * Purpose: The "routing table". Tells the system which MonsterASP database server belongs to which Company.
3. **[`Device.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/Device.cs):**
   * Fields: `DeviceId`, `CompanyId`, `DeviceCode`, `DeviceName`, `IsActive`.
   * Purpose: Tracks authorized computers, tablets, or POS terminals per company.

### B. Tenant Entities (Stored in Tenant DBs)
These entities track day-to-day store operations:
1. **[`Product.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/Product.cs):** `ProductId`, `ProductCode`, `ProductName`, `UnitPrice`, `IsActive`, `CreatedAt`.
2. **[`Customer.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/Customer.cs):** `CustomerId`, `CustomerCode`, `CustomerName`, `ContactNumber`, `EmailAddress`, `Address`.
3. **[`Supplier.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/Supplier.cs):** `SupplierId`, `SupplierCode`, `SupplierName`, `ContactPerson`, `ContactNumber`.
4. **[`Inventory.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.domain/entities/Inventory.cs):** `InventoryId`, `ProductId`, `QuantityOnHand`, `ReorderLevel`, `LastUpdatedAt`.

---

## 3. Layer 2: Infrastructure Layer (`ERP.infrastructure`)

The **Infrastructure layer** handles all database interactions using **Entity Framework Core (EF Core)**.

### A. The Two Database Contexts
1. **[`MasterErpDbContext.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/data/MasterErpDbContext.cs):**
   * Inherits from `IdentityDbContext`.
   * Connects exclusively to the central **Master Database** (`db67672`).
   * Configures primary keys, unique indexes (like unique `CompanyCode`), and foreign keys for `Company`, `CompanyDatabase`, and `Device`.
2. **[`TenantErpDbContext.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/data/TenantErpDbContext.cs):**
   * Inherits from `DbContext`.
   * Connects dynamically to any tenant database.
   * Manages `Products`, `Customers`, `Suppliers`, and `Inventories`.

---

### B. Dynamic Tenant Resolver & Factory Services (`ERP.infrastructure/services`)

In traditional apps, connection strings are hardcoded. In a multi-tenant app, **the database connection must be chosen dynamically at runtime based on which company is sending the request**.

Here is how the 4 classes work together:

#### 1. [`TenantDatabaseInfo.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/services/TenantDatabaseInfo.cs)
A simple data container holding `ServerName`, `DatabaseName`, and `CredentialKey`.

#### 2. [`ITenantDatabaseResolver.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/services/ITenantDatabaseResolver.cs) & [`TenantDatabaseResolver.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/services/TenantDatabaseResolver.cs)
* **What it does:** When an API endpoint receives `companyId: 1`, this service queries the `CompanyDatabases` table in the Master Database.
* **Result:** It finds:
  ```json
  {
    "ServerName": "db66559.public.databaseasp.net",
    "DatabaseName": "db66559",
    "CredentialKey": "TenantA"
  }
  ```

#### 3. [`ITenantDbContextFactory.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/services/ITenantDbContextFactory.cs) & [`TenantDbContextFactory.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.infrastructure/services/TenantDbContextFactory.cs)
* **What it does:** 
  1. Calls `TenantDatabaseResolver` to get the server and database name.
  2. Uses `CredentialKey` (e.g. `"TenantA"`) to safely read the SQL User ID and Password from `appsettings.json`.
  3. Assembles a live SQL connection string on the fly.
  4. Returns a fully connected `TenantErpDbContext` instance targeting that specific store's database!

---

## 4. Layer 3: API Layer (`ERP.api`)

The **API layer** exposes HTTP endpoints that your client apps (like WinForms or web apps) call.

### A. Configuration: [`appsettings.json`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.api/appsettings.json)
```json
{
  "ConnectionStrings": {
    "MasterErp": "Server=db67672.public.databaseasp.net; Database=db67672; User Id=db67672; Password=...; Encrypt=True; TrustServerCertificate=True;",
    "TenantErp": "Server=db66559.public.databaseasp.net; Database=db66559; User Id=db66559; Password=...; Encrypt=True; TrustServerCertificate=True;"
  },
  "TenantCredentials": {
    "TenantA": {
      "UserId": "db66559",
      "Password": "..."
    },
    "TenantB": {
      "UserId": "db66562",
      "Password": "..."
    }
  }
}
```

> ⚠️ **Critical Cloud Note: Why `.public.databaseasp.net`?**  
> MonsterASP databases have two addresses:
> 1. `dbXXXX.databaseasp.net`: Internal network address (resolves to private IP `10.0.0.31`). Only works when code is deployed inside MonsterASP.
> 2. `dbXXXX.public.databaseasp.net`: Public address. **Required** when connecting from your personal computer, Visual Studio, or SSMS!

---

### B. Dependency Injection & Endpoints: [`Program.cs`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.api/Program.cs)
1. **Service Registration:**
   ```csharp
   builder.Services.AddDbContext<MasterErpDbContext>(...);
   builder.Services.AddDbContext<TenantErpDbContext>(...);
   builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
   builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
   ```
2. **Key Endpoints Implemented:**
   * `POST /companies`: Creates a company in the Master DB.
   * `POST /devices`: Registers a device under a company.
   * `POST /company-databases`: Assigns a cloud database to a company.
   * `GET /test-tenant/{companyId}`: Tests dynamic factory connection to the tenant database.
   * `POST /tenant/{companyId}/products`: Adds products directly into that tenant's database.
   * `GET /tenant/{companyId}/products`: Retrieves all products for that specific tenant.
   * `POST & GET /tenant/{companyId}/customers`: Manages tenant customers.
   * `POST & GET /tenant/{companyId}/suppliers`: Manages tenant suppliers.

---

## 5. Layer 4: Interactive HTTP Testing (`ERP.api.http`)

Instead of needing Postman or third-party tools, Visual Studio has built-in `.http` file support:
* [`ERP.api.http`](file:///c:/Users/Cirunay/source/repos/IT8%20TechStore/ERP.api/ERP.api.http) contains 18 ready-to-run HTTP requests.
* Clicking **`Send Request`** executes the request directly against your running ASP.NET Core server and displays status (`201 Created`, `200 OK`) and JSON payloads.

---

## 6. Execution Walkthrough: The Complete Flow in Action

Here is exactly what happens when you clicked **Send Request** on `POST /companies`:

```
1. Click "Send Request" in ERP.api.http
   │
2. HTTP POST sent to https://localhost:7021/companies
   │
3. Program.cs matches route: app.MapPost("/companies", ...)
   │
4. EF Core injects MasterErpDbContext
   │
5. db.Companies.Add(company) tracks new company
   │
6. await db.SaveChangesAsync() executes SQL INSERT over the internet
   │
7. MonsterASP SQL Server executes:
   INSERT INTO [Companies] ([CompanyCode], [CompanyName], ...) VALUES ('COMP001', 'My First Company', ...)
   │
8. Database generates CompanyId: 1
   │
9. API returns HTTP 201 Created with { "companyId": 1, ... }
   │
10. SSMS shows the new row instantly upon query!
```

---

## 7. Summary & Ready for Micro Company

With this architecture completed:
1. **Master DB** is running live on MonsterASP with identity, companies, devices, and routing tables.
2. **Tenant A (Micro Store)** is configured with its own database connection.
3. The dynamic resolver and factory are tested and operating.
4. The system is ready to start building the Micro Company's POS, inventory adjustments, and WinForms desktop screens!
