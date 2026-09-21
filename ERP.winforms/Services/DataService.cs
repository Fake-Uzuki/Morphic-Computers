using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Data Access Service connecting ERP.winforms UI to ERP.api over HTTPS.
    /// Communicates with backend REST API using internal security authentication without exposing raw database credentials.
    /// </summary>
    public class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        private readonly ApiClient _apiClient = ApiClient.Instance;

        public List<Company> Companies { get; private set; } = new();
        public List<Product> Products { get; private set; } = new();
        public List<Category> Categories { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();
        public List<RepairTicket> RepairTickets { get; private set; } = new();
        public List<Supplier> Suppliers { get; private set; } = new();
        public List<StaffMember> StaffMembers { get; private set; } = new();
        public List<ApprovalRequest> ApprovalRequests { get; private set; } = new();
        public List<Customer> Customers { get; private set; } = new();
        public List<PayrollRecord> PayrollRecords { get; private set; } = new();
        public List<StorePolicy> StorePolicies { get; private set; } = new();

        public Action? CategoriesChanged;
        public Action? ProductsChanged;
        public Action? RepairTicketsChanged;
        public Action? SuppliersChanged;
        public Action? StaffMembersChanged;
        public Action? ApprovalRequestsChanged;
        public Action? CustomersChanged;
        public Action? PayrollRecordsChanged;
        public Action? StorePoliciesChanged;
        public Action<bool>? ConnectionStatusChanged;

        private int _activeCompanyId = 1;
        public int ActiveCompanyId
        {
            get => _activeCompanyId;
            set
            {
                if (_activeCompanyId != value)
                {
                    _activeCompanyId = value;
                    SwitchActiveTenantData();
                }
            }
        }
        public bool IsUsingLiveCloudDatabase { get; private set; }

        public Company? ActiveCompany => Companies.FirstOrDefault(c => c.CompanyId == ActiveCompanyId);

        public Company CurrentCompany
        {
            get => ActiveCompany ?? (Companies.Count > 0 ? Companies[0] : new Company { CompanyId = 1, CompanyName = "Tenant A", PlanName = "Micro" });
            set
            {
                if (value != null)
                {
                    ActiveCompanyId = value.CompanyId;
                }
            }
        }

        public void SwitchActiveTenantData()
        {
            Categories.Clear();
            Products.Clear();
            Orders.Clear();
            RepairTickets.Clear();
            Suppliers.Clear();
            StaffMembers.Clear();
            ApprovalRequests.Clear();
            Customers.Clear();
            PayrollRecords.Clear();
            StorePolicies.Clear();

            LoadCategoriesFromLocalCache();
            LoadProductsToLocalCache();
            SeedDefaultProductsIfEmpty();
            LoadOrdersFromLocalCache();
            LoadRepairsFromLocalCache();
            LoadSuppliersFromLocalCache();
            LoadStaffFromLocalCache();
            LoadApprovalsFromLocalCache();
            LoadCustomersFromLocalCache();
            LoadPayrollFromLocalCache();
            LoadPoliciesFromLocalCache();

            CategoriesChanged?.Invoke();
            ProductsChanged?.Invoke();
            RepairTicketsChanged?.Invoke();
            SuppliersChanged?.Invoke();
            StaffMembersChanged?.Invoke();
            ApprovalRequestsChanged?.Invoke();
            CustomersChanged?.Invoke();
            PayrollRecordsChanged?.Invoke();
            StorePoliciesChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => LoadFromDatabase());
            }
        }

        private DataService()
        {
            InitializeDataStore();
        }

        private void InitializeDataStore()
        {
            // Initial registered companies
            Companies = new List<Company>
            {
                new Company { CompanyId = 1, CompanyCode = "TENANT_A", CompanyName = "Tenant A", PlanName = "Micro", Description = "Micro Store Operations" },
                new Company { CompanyId = 2, CompanyCode = "TENANT_B", CompanyName = "Tenant B", PlanName = "SmallBusiness", Description = "Small Business Store Operations" },
                new Company { CompanyId = 3, CompanyCode = "TENANT_C", CompanyName = "Tenant C", PlanName = "Enterprise", Description = "Enterprise Store Operations" }
            };

            // Standard categories
            Categories = new List<Category>
            {
                new Category { Id = 1, CompanyId = 1, Name = "Graphics Cards (GPU)", Description = "Gaming & Productivity GPUs" },
                new Category { Id = 2, CompanyId = 1, Name = "Processors (CPU)", Description = "Intel & AMD Processors" },
                new Category { Id = 3, CompanyId = 1, Name = "Memory (RAM)", Description = "DDR4 & DDR5 RAM Kits" },
                new Category { Id = 4, CompanyId = 1, Name = "Storage (SSD/HDD)", Description = "NVMe SSDs & Hard Drives" },
                new Category { Id = 5, CompanyId = 1, Name = "Peripherals", Description = "Keyboards, Mice & Headsets" }
            };

            // Immediate local cache hydration: UI is instantly populated without waiting on network
            LoadCategoriesFromLocalCache();
            LoadProductsToLocalCache();
            SeedDefaultProductsIfEmpty();
            LoadOrdersFromLocalCache();
            LoadRepairsFromLocalCache();
            LoadSuppliersFromLocalCache();
            LoadStaffFromLocalCache();
            LoadApprovalsFromLocalCache();
            LoadCustomersFromLocalCache();
            LoadPayrollFromLocalCache();
            LoadPoliciesFromLocalCache();

            // Background / live cloud refresh (only if network is connected, off UI thread)
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => LoadFromDatabase());
            }
        }

        public void LoadFromDatabase()
        {
            // Ensure local cache is in memory immediately
            if (Categories.Count == 0) LoadCategoriesFromLocalCache();
            if (Products.Count == 0) LoadProductsToLocalCache();
            SeedDefaultProductsIfEmpty();
            if (Orders.Count == 0) LoadOrdersFromLocalCache();

            // Instant short-circuit if machine has no Wi-Fi / network connection
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                IsUsingLiveCloudDatabase = false;
                ConnectionStatusChanged?.Invoke(false);
                return;
            }

            try
            {
                // 1. Fetch live categories for the active tenant
                var liveCategories = Task.Run(() => _apiClient.GetCategoriesAsync(ActiveCompanyId)).GetAwaiter().GetResult();
                if (liveCategories == null || liveCategories.Count == 0)
                {
                    // Resilient direct DB fallback if ERP.api dev server is not running
                    liveCategories = Task.Run(() => TryDirectFetchCategoriesAsync(ActiveCompanyId)).GetAwaiter().GetResult();
                }

                if (liveCategories != null && liveCategories.Count > 0)
                {
                    Categories = liveCategories;
                    SaveCategoriesToLocalCache();
                    CategoriesChanged?.Invoke();
                }

                // 2. Fetch live products for the active tenant
                var liveProducts = Task.Run(() => _apiClient.GetProductsAsync(ActiveCompanyId)).GetAwaiter().GetResult();
                if (liveProducts == null || liveProducts.Count == 0)
                {
                    // Resilient direct DB fallback if ERP.api dev server is not running
                    liveProducts = Task.Run(() => TryDirectFetchProductsAsync(ActiveCompanyId)).GetAwaiter().GetResult();
                }

                if (liveProducts != null && liveProducts.Count > 0)
                {
                    Products = liveProducts
                        .GroupBy(p => p.ProductCode)
                        .Select(g => g.First())
                        .ToList();

                    // Normalize any legacy General or empty category to standard categories
                    foreach (var p in Products)
                    {
                        if (string.IsNullOrWhiteSpace(p.CategoryName) || p.CategoryName.Equals("General", StringComparison.OrdinalIgnoreCase))
                        {
                            string nameLower = (p.ProductName + " " + p.ProductCode).ToLowerInvariant();
                            if (nameLower.Contains("cpu") || nameLower.Contains("ryzen") || nameLower.Contains("intel") || nameLower.Contains("processor"))
                                p.CategoryName = "Processors (CPU)";
                            else if (nameLower.Contains("ram") || nameLower.Contains("ddr") || nameLower.Contains("memory"))
                                p.CategoryName = "Memory (RAM)";
                            else if (nameLower.Contains("ssd") || nameLower.Contains("hdd") || nameLower.Contains("storage") || nameLower.Contains("nvme"))
                                p.CategoryName = "Storage (SSD/HDD)";
                            else if (nameLower.Contains("mouse") || nameLower.Contains("keyboard") || nameLower.Contains("headset") || nameLower.Contains("peripheral"))
                                p.CategoryName = "Peripherals";
                            else if (nameLower.Contains("motherboard") || nameLower.Contains("board") || nameLower.Contains("b550") || nameLower.Contains("x570") || nameLower.Contains("z790"))
                                p.CategoryName = "Motherboards";
                            else
                                p.CategoryName = "Graphics Cards (GPU)";
                        }
                    }

                    SaveProductsToLocalCache();
                    IsUsingLiveCloudDatabase = true;
                    ProductsChanged?.Invoke();
                    ConnectionStatusChanged?.Invoke(true);
                }

                // 3. Fetch live orders for the active tenant through ERP.api / MonsterASP DB
                var liveOrders = Task.Run(() => _apiClient.GetOrdersAsync(ActiveCompanyId)).GetAwaiter().GetResult();
                if (liveOrders == null || liveOrders.Count == 0)
                {
                    liveOrders = Task.Run(() => TryDirectFetchOrdersAsync(ActiveCompanyId)).GetAwaiter().GetResult();
                }

                if (liveOrders != null && liveOrders.Count > 0)
                {
                    Orders = liveOrders
                        .GroupBy(o => o.Id)
                        .Select(g => g.First())
                        .OrderByDescending(o => o.CreatedAt)
                        .ToList();

                    SaveOrdersToLocalCache();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService.LoadFromDatabase note: {ex.Message}");
            }

            // If empty after online attempt, ensure local cache is loaded
            if (Categories.Count == 0) LoadCategoriesFromLocalCache();
            if (Products.Count == 0) LoadProductsToLocalCache();
            SeedDefaultProductsIfEmpty();
            if (Orders.Count == 0) LoadOrdersFromLocalCache();
            IsUsingLiveCloudDatabase = false;
            ConnectionStatusChanged?.Invoke(false);
        }

        private string GetLocalOrdersFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_orders.json");
        }

        private void SaveOrdersToLocalCache()
        {
            try
            {
                string path = GetLocalOrdersFilePath();
                string json = JsonSerializer.Serialize(Orders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveOrdersToLocalCache error: {ex.Message}");
            }
        }

        private void LoadOrdersFromLocalCache()
        {
            try
            {
                string path = GetLocalOrdersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Order>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Orders = cached
                            .GroupBy(o => o.Id)
                            .Select(g => g.First())
                            .OrderByDescending(o => o.CreatedAt)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadOrdersFromLocalCache error: {ex.Message}");
            }

            if (Orders.Count == 0)
            {
                SeedDefaultOrdersIfEmpty();
            }
        }

        private void SeedDefaultOrdersIfEmpty()
        {
            if (Orders.Count > 0) return;

            Orders = new List<Order>
            {
                // Today Orders
                new Order
                {
                    Id = "ORD-20260921-001",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Carlos Mendoza",
                    CreatedAt = DateTime.Now.AddHours(-1).AddMinutes(-15),
                    PaymentMethod = "Cash",
                    Subtotal = 329.99m,
                    Tax = 39.60m,
                    TotalAmount = 369.59m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 1, ProductName = "NVIDIA GeForce RTX 4060 8GB", Quantity = 1, UnitPrice = 329.99m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260921-002",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Apex Studio PH",
                    CreatedAt = DateTime.Now.AddHours(-4),
                    PaymentMethod = "Bank Transfer",
                    Subtotal = 15600.00m,
                    Tax = 1872.00m,
                    TotalAmount = 17472.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 2, ProductName = "Intel Core i5-13400 Processor", Quantity = 2, UnitPrice = 5600.00m },
                        new CartItem { ProductId = 4, ProductName = "Samsung 980 Pro 1TB NVMe SSD", Quantity = 1, UnitPrice = 4400.00m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260921-003",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Walk-in Gamer",
                    CreatedAt = DateTime.Now.AddHours(-6),
                    PaymentMethod = "GCash / E-Wallet",
                    Subtotal = 2104.99m,
                    Tax = 252.60m,
                    TotalAmount = 2357.59m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 20, ProductName = "AULA F75 Mechanical Keyboard", Quantity = 1, UnitPrice = 2015.00m },
                        new CartItem { ProductId = 7, ProductName = "Mechanical Gaming Keyboard RGB", Quantity = 1, UnitPrice = 89.99m }
                    }
                },

                // Yesterday Orders (This Week)
                new Order
                {
                    Id = "ORD-20260920-004",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Juan Dela Cruz",
                    CreatedAt = DateTime.Now.AddDays(-1).AddHours(-2),
                    PaymentMethod = "Cash",
                    Subtotal = 11400.00m,
                    Tax = 1368.00m,
                    TotalAmount = 12768.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 16, ProductName = "AMD Ryzen 5 5600", Quantity = 2, UnitPrice = 5700.00m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260920-005",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Maria Santos",
                    CreatedAt = DateTime.Now.AddDays(-1).AddHours(-5),
                    PaymentMethod = "Card / Terminal",
                    Subtotal = 8000.00m,
                    Tax = 960.00m,
                    TotalAmount = 8960.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 3, ProductName = "Corsair Vengeance 16GB DDR4 RAM", Quantity = 1, UnitPrice = 8000.00m }
                    }
                },

                // Earlier This Week
                new Order
                {
                    Id = "ORD-20260918-006",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "TechCorp Solutions PH",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    PaymentMethod = "Bank Transfer",
                    Subtotal = 21000.00m,
                    Tax = 2520.00m,
                    TotalAmount = 23520.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 21, ProductName = "NVDIA GeForce RTX 3080 8GB", Quantity = 7, UnitPrice = 3000.00m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260917-007",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Prime Rig Builders",
                    CreatedAt = DateTime.Now.AddDays(-4),
                    PaymentMethod = "Cash",
                    Subtotal = 16500.00m,
                    Tax = 1980.00m,
                    TotalAmount = 18480.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 15, ProductName = "MSI B550M PRO-VDH WIFI", Quantity = 3, UnitPrice = 5500.00m }
                    }
                },

                // Earlier This Month
                new Order
                {
                    Id = "ORD-20260912-008",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Cebu Esports Hub",
                    CreatedAt = DateTime.Now.AddDays(-9),
                    PaymentMethod = "Card / Terminal",
                    Subtotal = 18000.00m,
                    Tax = 2160.00m,
                    TotalAmount = 20160.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 17, ProductName = "Kingston Fury Beast 8GB DDR4", Quantity = 10, UnitPrice = 1800.00m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260908-009",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "DevOps Enterprise",
                    CreatedAt = DateTime.Now.AddDays(-13),
                    PaymentMethod = "Bank Transfer",
                    Subtotal = 30000.00m,
                    Tax = 3600.00m,
                    TotalAmount = 33600.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 4, ProductName = "Samsung 980 Pro 1TB NVMe SSD", Quantity = 3, UnitPrice = 10000.00m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260904-010",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Aegis Cyber Cafe",
                    CreatedAt = DateTime.Now.AddDays(-17),
                    PaymentMethod = "Cash",
                    Subtotal = 21600.00m,
                    Tax = 2592.00m,
                    TotalAmount = 24192.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 14, ProductName = "NVDIA GeForce RTX 4050 6GB", Quantity = 9, UnitPrice = 2400.00m }
                    }
                }
            };

            SaveOrdersToLocalCache();
        }

        private string GetLocalCategoriesFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_categories.json");
        }

        private void SaveCategoriesToLocalCache()
        {
            try
            {
                string path = GetLocalCategoriesFilePath();
                string json = JsonSerializer.Serialize(Categories, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCategoriesToLocalCache error: {ex.Message}");
            }
        }

        private void LoadCategoriesFromLocalCache()
        {
            try
            {
                string path = GetLocalCategoriesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Category>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Categories = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCategoriesFromLocalCache error: {ex.Message}");
            }
        }

        private string GetLocalProductsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_products.json");
        }

        private void SaveProductsToLocalCache()
        {
            try
            {
                string path = GetLocalProductsFilePath();
                string json = JsonSerializer.Serialize(Products, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveProductsToLocalCache error: {ex.Message}");
            }
        }

        private void LoadProductsToLocalCache()
        {
            try
            {
                string path = GetLocalProductsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Product>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Products = cached
                            .GroupBy(p => p.ProductCode)
                            .Select(g => g.First())
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProductsToLocalCache error: {ex.Message}");
            }

            SeedDefaultProductsIfEmpty();
        }

        private void SeedDefaultProductsIfEmpty()
        {
            if (Products.Count > 0) return;

            Products = new List<Product>
            {
                new Product { ProductId = 1, CompanyId = ActiveCompanyId, ProductCode = "GPU001", ProductName = "NVIDIA GeForce RTX 4060 8GB", UnitPrice = 329.99m, StockQuantity = 8, CategoryName = "Graphics Cards (GPU)", Description = "Ada Lovelace architecture with DLSS 3 support.", IsActive = true },
                new Product { ProductId = 2, CompanyId = ActiveCompanyId, ProductCode = "CPU001", ProductName = "Intel Core i5-13400 Processor", UnitPrice = 5600.00m, StockQuantity = 10, CategoryName = "Processors (CPU)", Description = "10-Core (6P+4E) Raptor Lake desktop processor.", IsActive = true },
                new Product { ProductId = 3, CompanyId = ActiveCompanyId, ProductCode = "RAM001", ProductName = "Corsair Vengeance 16GB DDR4 RAM", UnitPrice = 8000.00m, StockQuantity = 11, CategoryName = "Memory (RAM)", Description = "High performance DDR4 3200MHz memory module.", IsActive = true },
                new Product { ProductId = 4, CompanyId = ActiveCompanyId, ProductCode = "SSD001", ProductName = "Samsung 980 Pro 1TB NVMe SSD", UnitPrice = 10000.00m, StockQuantity = 9, CategoryName = "Storage (SSD/HDD)", Description = "PCIe Gen 4.0 NVMe M.2 solid state drive.", IsActive = true },
                new Product { ProductId = 5, CompanyId = ActiveCompanyId, ProductCode = "PSU001", ProductName = "Corsair 650W Power Supply", UnitPrice = 6000.00m, StockQuantity = 10, CategoryName = "Peripherals", Description = "80 PLUS Bronze certified continuous power supply.", IsActive = true },
                new Product { ProductId = 7, CompanyId = ActiveCompanyId, ProductCode = "KEY001", ProductName = "Mechanical Gaming Keyboard RGB", UnitPrice = 89.99m, StockQuantity = 7, CategoryName = "Peripherals", Description = "Customizable mechanical RGB gaming keyboard.", IsActive = true },
                new Product { ProductId = 8, CompanyId = ActiveCompanyId, ProductCode = "TEST-SSMS-001", ProductName = "SSMS Live GPU - Cloud Verified", UnitPrice = 750.00m, StockQuantity = 9, CategoryName = "Graphics Cards (GPU)", Description = "Cloud synchronized graphics hardware.", IsActive = true },
                new Product { ProductId = 11, CompanyId = ActiveCompanyId, ProductCode = "TEST-SSMS-004", ProductName = "SSMS Live Verification GPU", UnitPrice = 580.00m, StockQuantity = 1180, CategoryName = "Graphics Cards (GPU)", Description = "Enterprise verified graphics card.", IsActive = true },
                new Product { ProductId = 14, CompanyId = ActiveCompanyId, ProductCode = "GPU002", ProductName = "NVDIA GeForce RTX 4050 6GB", UnitPrice = 2400.00m, StockQuantity = 9, CategoryName = "Graphics Cards (GPU)", Description = "Dedicated gaming and creator graphics processor.", IsActive = true },
                new Product { ProductId = 15, CompanyId = ActiveCompanyId, ProductCode = "MB001", ProductName = "MSI B550M PRO-VDH WIFI", UnitPrice = 5500.00m, StockQuantity = 16, CategoryName = "Motherboards", Description = "AMD AM4 micro-ATX motherboard with Wi-Fi.", IsActive = true },
                new Product { ProductId = 16, CompanyId = ActiveCompanyId, ProductCode = "CPU002", ProductName = "AMD Ryzen 5 5600", UnitPrice = 5700.00m, StockQuantity = 8, CategoryName = "Processors (CPU)", Description = "6-Core 12-Thread unlocked desktop processor.", IsActive = true },
                new Product { ProductId = 17, CompanyId = ActiveCompanyId, ProductCode = "RAM002", ProductName = "Kingston Fury Beast 8GB DDR4", UnitPrice = 1800.00m, StockQuantity = 20, CategoryName = "Memory (RAM)", Description = "Reliable 3200MHz DDR4 gaming memory stick.", IsActive = true },
                new Product { ProductId = 18, CompanyId = ActiveCompanyId, ProductCode = "PSU003", ProductName = "ASUS Prime 750W Bronze", UnitPrice = 3300.00m, StockQuantity = 10, CategoryName = "Graphics Cards (GPU)", Description = "750W 80 PLUS Bronze power unit.", IsActive = true },
                new Product { ProductId = 19, CompanyId = ActiveCompanyId, ProductCode = "PSU002", ProductName = "MSI MAG A650BN 650W Bronze", UnitPrice = 2700.00m, StockQuantity = 12, CategoryName = "Graphics Cards (GPU)", Description = "650W Bronze ATX high efficiency power supply.", IsActive = true },
                new Product { ProductId = 20, CompanyId = ActiveCompanyId, ProductCode = "KEY002", ProductName = "AULA F75 Mechanical Keyboard", UnitPrice = 2015.00m, StockQuantity = 10, CategoryName = "Peripherals", Description = "Gasket mount 75% mechanical wireless keyboard.", IsActive = true },
                new Product { ProductId = 21, CompanyId = ActiveCompanyId, ProductCode = "GPU004", ProductName = "NVDIA GeForce RTX 3080 8GB", UnitPrice = 3000.00m, StockQuantity = 10, CategoryName = "Graphics Cards (GPU)", Description = "High-end Ampere architecture graphics card.", IsActive = true }
            };

            SaveProductsToLocalCache();
        }

        private static string GetTenantConnectionString(int companyId)
        {
            return companyId == 2
                ? "Server=db67675.databaseasp.net;Database=db67675;User Id=db67675;Password=k@4FMx=9d6E%;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=2;"
                : "Server=db67673.public.databaseasp.net;Database=db67673;User Id=db67673;Password=Wt7-8=mFA3#i;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=2;";
        }

        private async Task<List<Category>?> TryDirectFetchCategoriesAsync(int companyId)
        {
            try
            {
                var options = new DbContextOptionsBuilder<TenantErpDbContext>()
                    .UseSqlServer(GetTenantConnectionString(companyId))
                    .Options;
                using var db = new TenantErpDbContext(options);
                return await db.Categories
                    .AsNoTracking()
                    .Where(c => c.CompanyId == companyId)
                    .OrderBy(c => c.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryDirectFetchCategories error: {ex.Message}");
                return null;
            }
        }

        private async Task<List<Product>?> TryDirectFetchProductsAsync(int companyId)
        {
            try
            {
                var options = new DbContextOptionsBuilder<TenantErpDbContext>()
                    .UseSqlServer(GetTenantConnectionString(companyId))
                    .Options;
                using var db = new TenantErpDbContext(options);
                var products = await db.Products.AsNoTracking().OrderBy(x => x.ProductId).ToListAsync();
                var inventories = await db.Inventories.AsNoTracking().ToListAsync();
                foreach (var p in products)
                {
                    var inv = inventories.FirstOrDefault(i => i.ProductId == p.ProductId);
                    if (inv != null) p.StockQuantity = (int)inv.QuantityOnHand;
                }
                return products;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryDirectFetchProducts error: {ex.Message}");
                return null;
            }
        }

        private async Task<List<Order>?> TryDirectFetchOrdersAsync(int companyId)
        {
            try
            {
                var options = new DbContextOptionsBuilder<TenantErpDbContext>()
                    .UseSqlServer(GetTenantConnectionString(companyId))
                    .Options;
                using var db = new TenantErpDbContext(options);
                return await db.Orders
                    .AsNoTracking()
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryDirectFetchOrders error: {ex.Message}");
                return null;
            }
        }

        public static readonly HashSet<string> DefaultPresetNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Graphics Cards (GPU)",
            "Processors (CPU)",
            "Memory (RAM)",
            "Storage (SSD/HDD)",
            "Peripherals"
        };

        public bool IsDefaultPreset(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return false;
            return DefaultPresetNames.Contains(categoryName.Trim());
        }

        public bool AddCategory(Category category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name)) return false;
            category.Name = category.Name.Trim();

            var existing = Categories.FirstOrDefault(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return true;

            int nextId = Categories.Count > 0 ? Categories.Max(c => c.Id) + 1 : 1;
            category.Id = nextId;
            category.CompanyId = ActiveCompanyId;
            Categories.Add(category);
            SaveCategoriesToLocalCache();
            CategoriesChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var res = await _apiClient.AddCategoryAsync(ActiveCompanyId, category);
                        if (res == null)
                        {
                            SyncManager.Instance.EnqueueCategory(category, false, ActiveCompanyId);
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddCategory API error: {ex.Message}");
                    SyncManager.Instance.EnqueueCategory(category, false, ActiveCompanyId);
                }
            }
            else
            {
                SyncManager.Instance.EnqueueCategory(category, false, ActiveCompanyId);
            }

            return true;
        }

        public bool UpdateCategory(int categoryId, string newName, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(newName)) return false;
            newName = newName.Trim();

            var cat = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (cat == null) return false;

            // Check duplicate
            var dup = Categories.FirstOrDefault(c => c.Id != categoryId && c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (dup != null) return false;

            string oldName = cat.Name;
            cat.Name = newName;
            if (description != null) cat.Description = description.Trim();

            SaveCategoriesToLocalCache();

            // Cascade update to in-memory products
            if (!oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var p in Products.Where(p => p.CategoryName.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                {
                    p.CategoryName = newName;
                }
            }

            CategoriesChanged?.Invoke();

            // Dispatch to ERP.api asynchronously or enqueue offline
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var res = await _apiClient.UpdateCategoryAsync(ActiveCompanyId, categoryId, cat);
                        if (res == null)
                        {
                            SyncManager.Instance.EnqueueCategory(cat, true, ActiveCompanyId);
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCategory API error: {ex.Message}");
                    SyncManager.Instance.EnqueueCategory(cat, true, ActiveCompanyId);
                }
            }
            else
            {
                SyncManager.Instance.EnqueueCategory(cat, true, ActiveCompanyId);
            }

            return true;
        }

        public bool DeleteCategory(int categoryId, string? reassignTo = null)
        {
            var cat = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (cat == null) return false;

            if (IsDefaultPreset(cat.Name)) return false; // Protected

            string fallback = string.IsNullOrWhiteSpace(reassignTo) ? "Graphics Cards (GPU)" : reassignTo.Trim();
            string oldName = cat.Name;

            // Reassign in-memory products
            foreach (var p in Products.Where(p => p.CategoryName.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
            {
                p.CategoryName = fallback;
            }

            Categories.Remove(cat);
            SaveCategoriesToLocalCache();
            CategoriesChanged?.Invoke();

            // Dispatch to ERP.api asynchronously or enqueue offline
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Task.Run(async () =>
                    {
                        bool ok = await _apiClient.DeleteCategoryAsync(ActiveCompanyId, categoryId, fallback);
                        if (!ok)
                        {
                            SyncManager.Instance.EnqueueDeleteCategory(categoryId, fallback, ActiveCompanyId);
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteCategory API error: {ex.Message}");
                    SyncManager.Instance.EnqueueDeleteCategory(categoryId, fallback, ActiveCompanyId);
                }
            }
            else
            {
                SyncManager.Instance.EnqueueDeleteCategory(categoryId, fallback, ActiveCompanyId);
            }

            return true;
        }

        public bool AddProduct(Product product) => SaveProduct(product);

        public bool SaveProduct(Product product)
        {
            try
            {
                // Check if already in memory
                var existing = Products.FirstOrDefault(p =>
                    (product.ProductId > 0 && p.ProductId == product.ProductId) ||
                    p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    return UpdateProduct(product);
                }

                // Send to ERP.api safely off UI thread only if network is available
                Product? created = null;
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    created = Task.Run(() => _apiClient.AddProductAsync(ActiveCompanyId, product)).GetAwaiter().GetResult();
                }

                if (created != null)
                {
                    product.ProductId = created.ProductId;
                }
                else
                {
                    // Enqueue for offline sync when online
                    SyncManager.Instance.EnqueueProduct(product, false, ActiveCompanyId);
                    if (product.ProductId == 0)
                    {
                        int nextId = Products.Count > 0 ? Products.Max(p => p.ProductId) + 1 : 1;
                        product.ProductId = nextId;
                    }
                }

                Products.Add(product);
                SaveProductsToLocalCache();
                ProductsChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveProduct error: {ex.Message}");
                SyncManager.Instance.EnqueueProduct(product, false, ActiveCompanyId);

                if (!Products.Any(p => p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase)))
                {
                    int nextId = Products.Count > 0 ? Products.Max(p => p.ProductId) + 1 : 1;
                    product.ProductId = nextId;
                    Products.Add(product);
                }

                SaveProductsToLocalCache();
                ProductsChanged?.Invoke();
                return true;
            }
        }

        public bool UpdateProduct(Product product)
        {
            var existing = Products.FirstOrDefault(p =>
                (product.ProductId > 0 && p.ProductId == product.ProductId) ||
                p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.ProductName = product.ProductName;
                existing.ProductCode = product.ProductCode;
                existing.UnitPrice = product.UnitPrice;
                existing.StockQuantity = product.StockQuantity;
                existing.CategoryName = product.CategoryName;
                existing.Description = product.Description;
                existing.IsActive = product.IsActive;
                existing.SupplierName = product.SupplierName;
                existing.SupplierId = product.SupplierId;
            }

            try
            {
                bool synced = false;
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    synced = Task.Run(() => _apiClient.UpdateProductAsync(ActiveCompanyId, product)).GetAwaiter().GetResult();
                }
                if (!synced)
                {
                    SyncManager.Instance.EnqueueProduct(product, true, ActiveCompanyId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProduct API error: {ex.Message}");
                SyncManager.Instance.EnqueueProduct(product, true, ActiveCompanyId);
            }

            // Ensure deduplication in memory
            Products = Products
                .GroupBy(p => p.ProductCode)
                .Select(g => g.First())
                .ToList();

            // Immediately persist changes to local storage
            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();

            return true;
        }

        public bool ArchiveProduct(int productId)
        {
            var p = Products.FirstOrDefault(x => x.ProductId == productId);
            if (p == null) return false;

            p.IsActive = false;
            p.ArchivedAt = DateTime.UtcNow;

            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Task.Run(async () =>
                    {
                        bool ok = await _apiClient.ArchiveProductAsync(ActiveCompanyId, productId);
                        if (!ok)
                        {
                            SyncManager.Instance.EnqueueArchiveProduct(productId, ActiveCompanyId);
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ArchiveProduct API error: {ex.Message}");
                    SyncManager.Instance.EnqueueArchiveProduct(productId, ActiveCompanyId);
                }
            }
            else
            {
                SyncManager.Instance.EnqueueArchiveProduct(productId, ActiveCompanyId);
            }

            return true;
        }

        public bool RestoreProduct(int productId)
        {
            var p = Products.FirstOrDefault(x => x.ProductId == productId);
            if (p == null) return false;

            p.IsActive = true;
            p.ArchivedAt = null;

            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Task.Run(async () =>
                    {
                        bool ok = await _apiClient.RestoreProductAsync(ActiveCompanyId, productId);
                        if (!ok)
                        {
                            SyncManager.Instance.EnqueueRestoreProduct(productId, ActiveCompanyId);
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RestoreProduct API error: {ex.Message}");
                    SyncManager.Instance.EnqueueRestoreProduct(productId, ActiveCompanyId);
                }
            }
            else
            {
                SyncManager.Instance.EnqueueRestoreProduct(productId, ActiveCompanyId);
            }

            return true;
        }

        /// <summary>
        /// Enterprise Soft Delete: Archives product instead of permanently deleting to protect order audit history.
        /// </summary>
        public bool DeleteProduct(int productId) => ArchiveProduct(productId);

        public void ProcessOrder(Order order)
        {
            // Prevent duplicate insertions
            if (!Orders.Any(o => o.Id == order.Id))
            {
                Orders.Insert(0, order);
            }

            // Deduct stock for purchased items locally
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity = Math.Max(0, prod.StockQuantity - item.Quantity);
                }
            }

            // Save immediately to local persistent cache so transactions and stock levels are never lost
            SaveOrdersToLocalCache();
            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();

            // Sync with ERP.api / MonsterASP DB safely off UI thread
            try
            {
                bool synced = false;
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    synced = Task.Run(() => _apiClient.ProcessOrderAsync(ActiveCompanyId, order)).GetAwaiter().GetResult();
                }
                if (!synced)
                {
                    SyncManager.Instance.EnqueueOrder(order, ActiveCompanyId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessOrder API error: {ex.Message}");
                SyncManager.Instance.EnqueueOrder(order, ActiveCompanyId);
            }
        }

        public bool VoidOrder(string orderId)
        {
            string cleanId = orderId.TrimStart('#');
            var order = Orders.FirstOrDefault(o => o.Id.TrimStart('#').Equals(cleanId, StringComparison.OrdinalIgnoreCase));
            if (order == null) return false;

            order.Status = "Voided";
            order.ArchivedAt = DateTime.UtcNow;

            // Restock items in inventory
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity += item.Quantity;
                }
            }

            SaveOrdersToLocalCache();
            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();
            return true;
        }

        public bool RestoreOrder(string orderId)
        {
            string cleanId = orderId.TrimStart('#');
            var order = Orders.FirstOrDefault(o => o.Id.TrimStart('#').Equals(cleanId, StringComparison.OrdinalIgnoreCase));
            if (order == null) return false;

            order.Status = "Completed";
            order.ArchivedAt = null;

            // Re-deduct stock
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity = Math.Max(0, prod.StockQuantity - item.Quantity);
                }
            }

            SaveOrdersToLocalCache();
            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Flushes all in-memory products, categories, and orders to local disk files.
        /// Called automatically during form closing and application exit.
        /// </summary>
        public void SaveAllToDisk()
        {
            try
            {
                SaveProductsToLocalCache();
                SaveCategoriesToLocalCache();
                SaveOrdersToLocalCache();
                SaveRepairsToLocalCache();
                SaveSuppliersToLocalCache();
                SaveStaffToLocalCache();
                SaveApprovalsToLocalCache();
                SaveCustomersToLocalCache();
                SavePayrollToLocalCache();
                SavePoliciesToLocalCache();
                System.Diagnostics.Debug.WriteLine("DataService.SaveAllToDisk: Successfully persisted all data to disk.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService.SaveAllToDisk error: {ex.Message}");
            }
        }

        /// <summary>
        /// Isolated Tenant Backup: Exports all Tenant A store data (Products, Orders) into a portable JSON backup file.
        /// </summary>
        public string ExportTenantBackup(string destinationPath)
        {
            var backupData = new
            {
                BackupDate = DateTime.UtcNow,
                Tenant = CurrentCompany.CompanyName,
                TotalProducts = Products.Count,
                TotalOrders = Orders.Count,
                TotalRevenue = GetTotalRevenue(),
                Products = Products.Select(p => new
                {
                    p.ProductId,
                    p.ProductCode,
                    p.ProductName,
                    p.UnitPrice,
                    p.StockQuantity,
                    p.CategoryName,
                    p.IsActive
                }),
                Orders = Orders.Select(o => new
                {
                    o.Id,
                    o.CustomerName,
                    o.CreatedAt,
                    o.TotalAmount,
                    o.PaymentMethod,
                    ItemCount = o.Items.Count
                })
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(backupData, jsonOptions);
            File.WriteAllText(destinationPath, json);
            return destinationPath;
        }

        // =========================================================================
        // TENANT B: SERVICE & REPAIR MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalRepairsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_repairs.json");
        }

        public void SaveRepairsToLocalCache()
        {
            try
            {
                string path = GetLocalRepairsFilePath();
                string json = JsonSerializer.Serialize(RepairTickets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveRepairsToLocalCache error: {ex.Message}");
            }
        }

        public void LoadRepairsFromLocalCache()
        {
            try
            {
                string path = GetLocalRepairsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<RepairTicket>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        RepairTickets = cached;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRepairsFromLocalCache error: {ex.Message}");
            }

            SeedDefaultRepairsIfEmpty();
        }

        private void SeedDefaultRepairsIfEmpty()
        {
            if (RepairTickets.Count > 0) return;

            // Seed realistic diagnostic repair bench jobs for Tenant B (Small Business)
            RepairTickets = new List<RepairTicket>
            {
                new RepairTicket
                {
                    RepairTicketId = 1,
                    TicketNumber = "REP-20260918-101",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Marco Valderrama",
                    CustomerPhone = "0917-555-8912",
                    CustomerEmail = "marco.v@gmail.com",
                    DeviceType = "Desktop PC",
                    DeviceBrandModel = "Custom Rig (Ryzen 7 5800X / RTX 3070)",
                    SerialNumber = "SN-CR-9921",
                    ReportedIssue = "No display on boot; fans spin for 3 seconds then stop.",
                    DiagnosticNotes = "Tested with bench PSU - OK. GPU seating checked - suspect PCIe slot riser or memory training failure.",
                    AssignedTechnician = "Lead Tech Alex",
                    Status = "Diagnosing",
                    LaborFee = 1500.00m,
                    PartsCost = 0.00m,
                    DepositAmount = 500.00m,
                    CreatedAt = DateTime.UtcNow.AddHours(-14),
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(2)
                },
                new RepairTicket
                {
                    RepairTicketId = 2,
                    TicketNumber = "REP-20260918-102",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Sarah Jenkins",
                    CustomerPhone = "0928-771-3344",
                    CustomerEmail = "sarah.j@techhub.ph",
                    DeviceType = "Laptop",
                    DeviceBrandModel = "Lenovo Legion 5 15ACH6",
                    SerialNumber = "PF2A190X",
                    ReportedIssue = "165Hz IPS screen flickers black when hinge is tilted past 90 degrees.",
                    DiagnosticNotes = "EDP ribbon cable pinched in left hinge. Replacement cable and panel tested working.",
                    AssignedTechnician = "Tech Justin",
                    Status = "ReadyForPickup",
                    LaborFee = 1200.00m,
                    PartsCost = 4500.00m,
                    DepositAmount = 2000.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    CompletedAt = DateTime.UtcNow.AddHours(-3)
                },
                new RepairTicket
                {
                    RepairTicketId = 3,
                    TicketNumber = "REP-20260918-103",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "David Lim",
                    CustomerPhone = "0918-333-7890",
                    CustomerEmail = "david.lim@outlook.com",
                    DeviceType = "Graphics Card (GPU)",
                    DeviceBrandModel = "ASUS ROG Strix RTX 3080 OC 10GB",
                    SerialNumber = "K12M-STRIX3080",
                    ReportedIssue = "Overheating, thermal throttling at 95°C under gaming load. Middle fan noisy.",
                    DiagnosticNotes = "Requires full thermal pad & paste replacement + replacement 95mm fan blade.",
                    AssignedTechnician = "Lead Tech Alex",
                    Status = "InRepair",
                    LaborFee = 2000.00m,
                    PartsCost = 1850.00m,
                    DepositAmount = 1000.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(1)
                },
                new RepairTicket
                {
                    RepairTicketId = 4,
                    TicketNumber = "REP-20260918-104",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Patricia Santos",
                    CustomerPhone = "0905-224-8899",
                    CustomerEmail = "patricia.s@bpo-center.com",
                    DeviceType = "Laptop",
                    DeviceBrandModel = "Dell Latitude 5420",
                    SerialNumber = "8G3HKL2",
                    ReportedIssue = "Battery swelling trackpad; won't hold charge without AC power.",
                    DiagnosticNotes = "Swollen 4-cell 63Wh battery removed safely. Awaiting delivery of OEM replacement battery.",
                    AssignedTechnician = "Tech Justin",
                    Status = "AwaitingParts",
                    LaborFee = 800.00m,
                    PartsCost = 3200.00m,
                    DepositAmount = 1500.00m,
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(3)
                }
            };

            SaveRepairsToLocalCache();
        }

        public void AddRepairTicket(RepairTicket ticket)
        {
            if (ticket == null) return;
            ticket.RepairTicketId = (RepairTickets.Count > 0 ? RepairTickets.Max(t => t.RepairTicketId) : 0) + 1;
            ticket.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                ticket.TicketNumber = $"REP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.IsActive = true;

            RepairTickets.Insert(0, ticket);
            SaveRepairsToLocalCache();
            RepairTicketsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.CreateRepairTicketAsync(ActiveCompanyId, ticket));
            }
        }

        public void UpdateRepairStatus(int ticketId, string status, string? notes = null, string? technician = null)
        {
            var ticket = RepairTickets.FirstOrDefault(t => t.RepairTicketId == ticketId);
            if (ticket == null) return;

            ticket.Status = status;
            if (!string.IsNullOrEmpty(notes)) ticket.DiagnosticNotes = notes;
            if (!string.IsNullOrEmpty(technician)) ticket.AssignedTechnician = technician;
            if (status == "Completed" || status == "ReadyForPickup")
            {
                ticket.CompletedAt = DateTime.UtcNow;
            }

            SaveRepairsToLocalCache();
            RepairTicketsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.UpdateRepairStatusAsync(ActiveCompanyId, ticketId, status, notes, technician));
            }
        }

        public void UpdateRepairBilling(int ticketId, decimal labor, decimal parts, decimal deposit)
        {
            var ticket = RepairTickets.FirstOrDefault(t => t.RepairTicketId == ticketId);
            if (ticket == null) return;

            ticket.LaborFee = labor;
            ticket.PartsCost = parts;
            ticket.DepositAmount = deposit;

            SaveRepairsToLocalCache();
            RepairTicketsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.UpdateRepairBillingAsync(ActiveCompanyId, ticketId, labor, parts, deposit));
            }
        }

        // =========================================================================
        // TENANT B: SUPPLIER MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalSuppliersFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_suppliers.json");
        }

        public void SaveSuppliersToLocalCache()
        {
            try
            {
                string path = GetLocalSuppliersFilePath();
                string json = JsonSerializer.Serialize(Suppliers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSuppliersToLocalCache error: {ex.Message}");
            }
        }

        public void LoadSuppliersFromLocalCache()
        {
            try
            {
                string path = GetLocalSuppliersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Supplier>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Suppliers = cached.Where(s => s.IsActive).ToList();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSuppliersFromLocalCache error: {ex.Message}");
            }

            SeedDefaultSuppliersIfEmpty();
        }

        private void SeedDefaultSuppliersIfEmpty()
        {
            if (Suppliers.Count > 0) return;

            Suppliers = new List<Supplier>
            {
                new Supplier
                {
                    SupplierId = 1,
                    SupplierCode = "SUP-101",
                    SupplierName = "ASUS Direct Components Distribution",
                    ContactPerson = "Michael Tan (Senior Key Account Mgr)",
                    ContactNumber = "0917-882-9011",
                    EmailAddress = "orders@asusdistro.ph",
                    Address = "Building 4, Megacenter Hub, Mandaluyong City",
                    IsActive = true
                },
                new Supplier
                {
                    SupplierId = 2,
                    SupplierCode = "SUP-102",
                    SupplierName = "Corsair & Kingston Parts Direct PH",
                    ContactPerson = "Elena Gomez (Channel Logistics)",
                    ContactNumber = "0920-554-1234",
                    EmailAddress = "elena@corsairdirect.ph",
                    Address = "Ortigas Business Center, Pasig City",
                    IsActive = true
                },
                new Supplier
                {
                    SupplierId = 3,
                    SupplierCode = "SUP-103",
                    SupplierName = "CyberPower Tech & Repair Supply Co.",
                    ContactPerson = "Arthur Reyes (Wholesale Parts Head)",
                    ContactNumber = "0918-331-4567",
                    EmailAddress = "sales@cyberpowerparts.ph",
                    Address = "Gilmore IT Center, New Manila, Quezon City",
                    IsActive = true
                },
                new Supplier
                {
                    SupplierId = 4,
                    SupplierCode = "SUP-104",
                    SupplierName = "Cooler Master & Lian Li Distro",
                    ContactPerson = "Grace Villar",
                    ContactNumber = "0919-444-2211",
                    EmailAddress = "grace@coolermasterdistro.ph",
                    Address = "BGC Corporate Center, Taguig City",
                    IsActive = true
                }
            };

            SaveSuppliersToLocalCache();
        }

        public void AddSupplier(Supplier supplier)
        {
            if (supplier == null) return;
            supplier.SupplierId = (Suppliers.Count > 0 ? Suppliers.Max(s => s.SupplierId) : 0) + 1;
            if (string.IsNullOrWhiteSpace(supplier.SupplierCode))
            {
                supplier.SupplierCode = $"SUP-{new Random().Next(100, 999)}";
            }
            supplier.CreatedAt = DateTime.UtcNow;
            supplier.IsActive = true;

            Suppliers.Add(supplier);
            SaveSuppliersToLocalCache();
            SuppliersChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.CreateSupplierAsync(ActiveCompanyId, supplier));
            }
        }

        public void UpdateSupplier(Supplier supplier)
        {
            if (supplier == null) return;
            var existing = Suppliers.FirstOrDefault(s => s.SupplierId == supplier.SupplierId);
            if (existing != null)
            {
                existing.SupplierName = supplier.SupplierName;
                existing.ContactPerson = supplier.ContactPerson;
                existing.ContactNumber = supplier.ContactNumber;
                existing.EmailAddress = supplier.EmailAddress;
                existing.Address = supplier.Address;

                SaveSuppliersToLocalCache();
                SuppliersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.UpdateSupplierAsync(ActiveCompanyId, supplier.SupplierId, existing));
                }
            }
        }

        public void DeleteSupplier(int supplierId)
        {
            var existing = Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
            if (existing != null)
            {
                existing.IsActive = false;
                Suppliers.Remove(existing);
                SaveSuppliersToLocalCache();
                SuppliersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.DeleteSupplierAsync(ActiveCompanyId, supplierId));
                }
            }
        }

        // =========================================================================
        // TENANT B: STAFF MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalStaffFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_staff.json");
        }

        public void SaveStaffToLocalCache()
        {
            try
            {
                string path = GetLocalStaffFilePath();
                string json = JsonSerializer.Serialize(StaffMembers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveStaffToLocalCache error: {ex.Message}");
            }
        }

        public void LoadStaffFromLocalCache()
        {
            try
            {
                string path = GetLocalStaffFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<StaffMember>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        StaffMembers = cached.Where(s => s.IsActive).ToList();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStaffFromLocalCache error: {ex.Message}");
            }

            SeedDefaultStaffIfEmpty();
        }

        private void SeedDefaultStaffIfEmpty()
        {
            if (StaffMembers.Count > 0) return;

            StaffMembers = new List<StaffMember>
            {
                new StaffMember
                {
                    StaffId = 1,
                    CompanyId = ActiveCompanyId,
                    StaffCode = "EMP-1001",
                    FullName = "Cirunay",
                    Username = "cirunay",
                    Role = "Store Administrator",
                    PositionTitle = "Owner & General Manager",
                    Email = "cirunay@morphic.ph",
                    PhoneNumber = "0917-000-1122",
                    HourlyRate = 250.00m,
                    MonthlySalary = 45000.00m,
                    IsActive = true,
                    HiredDate = DateTime.UtcNow.AddMonths(-18)
                },
                new StaffMember
                {
                    StaffId = 2,
                    CompanyId = ActiveCompanyId,
                    StaffCode = "EMP-1002",
                    FullName = "Marcus Vance",
                    Username = "manager",
                    Role = "Store Manager",
                    PositionTitle = "Store & Inventory Manager",
                    Email = "marcus@morphic.ph",
                    PhoneNumber = "0918-111-2233",
                    HourlyRate = 200.00m,
                    MonthlySalary = 35000.00m,
                    IsActive = true,
                    HiredDate = DateTime.UtcNow.AddMonths(-12)
                },
                new StaffMember
                {
                    StaffId = 3,
                    CompanyId = ActiveCompanyId,
                    StaffCode = "EMP-1003",
                    FullName = "Alex Rodriguez",
                    Username = "tech",
                    Role = "Hardware Technician",
                    PositionTitle = "Senior Bench Technician",
                    Email = "alex.tech@morphic.ph",
                    PhoneNumber = "0920-222-3344",
                    HourlyRate = 160.00m,
                    MonthlySalary = 28000.00m,
                    IsActive = true,
                    HiredDate = DateTime.UtcNow.AddMonths(-8)
                },
                new StaffMember
                {
                    StaffId = 4,
                    CompanyId = ActiveCompanyId,
                    StaffCode = "EMP-1004",
                    FullName = "Justin Morales",
                    Username = "justin",
                    Role = "Hardware Technician",
                    PositionTitle = "Assembly & Diagnostics Tech",
                    Email = "justin.m@morphic.ph",
                    PhoneNumber = "0922-333-4455",
                    HourlyRate = 145.00m,
                    MonthlySalary = 25000.00m,
                    IsActive = true,
                    HiredDate = DateTime.UtcNow.AddMonths(-5)
                },
                new StaffMember
                {
                    StaffId = 5,
                    CompanyId = ActiveCompanyId,
                    StaffCode = "EMP-1005",
                    FullName = "Camille Dizon",
                    Username = "cashier",
                    Role = "Cashier Operations",
                    PositionTitle = "Lead POS Cashier",
                    Email = "camille@morphic.ph",
                    PhoneNumber = "0927-444-5566",
                    HourlyRate = 130.00m,
                    MonthlySalary = 22000.00m,
                    IsActive = true,
                    HiredDate = DateTime.UtcNow.AddMonths(-4)
                }
            };

            SaveStaffToLocalCache();
        }

        public void AddStaffMember(StaffMember staff)
        {
            if (staff == null) return;
            staff.StaffId = (StaffMembers.Count > 0 ? StaffMembers.Max(s => s.StaffId) : 0) + 1;
            staff.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(staff.StaffCode))
            {
                staff.StaffCode = $"EMP-{new Random().Next(1000, 9999)}";
            }
            staff.HiredDate = DateTime.UtcNow;
            staff.IsActive = true;

            StaffMembers.Add(staff);
            SaveStaffToLocalCache();
            StaffMembersChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.CreateStaffAsync(ActiveCompanyId, staff));
            }
        }

        public void UpdateStaffMember(StaffMember staff)
        {
            if (staff == null) return;
            var existing = StaffMembers.FirstOrDefault(s => s.StaffId == staff.StaffId);
            if (existing != null)
            {
                existing.FullName = staff.FullName;
                existing.Role = staff.Role;
                existing.PositionTitle = staff.PositionTitle;
                existing.Email = staff.Email;
                existing.PhoneNumber = staff.PhoneNumber;
                existing.HourlyRate = staff.HourlyRate;
                existing.MonthlySalary = staff.MonthlySalary;

                SaveStaffToLocalCache();
                StaffMembersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.UpdateStaffAsync(ActiveCompanyId, staff.StaffId, existing));
                }
            }
        }

        public void DeactivateStaffMember(int staffId)
        {
            var existing = StaffMembers.FirstOrDefault(s => s.StaffId == staffId);
            if (existing != null)
            {
                existing.IsActive = false;
                StaffMembers.Remove(existing);
                SaveStaffToLocalCache();
                StaffMembersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.DeactivateStaffAsync(ActiveCompanyId, staffId));
                }
            }
        }

        // =========================================================================
        // TENANT B: WORKFLOW & APPROVAL CACHING & CRUD
        // =========================================================================
        private string GetLocalApprovalsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_approvals.json");
        }

        public void SaveApprovalsToLocalCache()
        {
            try
            {
                string path = GetLocalApprovalsFilePath();
                string json = JsonSerializer.Serialize(ApprovalRequests, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveApprovalsToLocalCache error: {ex.Message}");
            }
        }

        public void LoadApprovalsFromLocalCache()
        {
            try
            {
                string path = GetLocalApprovalsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<ApprovalRequest>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        ApprovalRequests = cached;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadApprovalsFromLocalCache error: {ex.Message}");
            }

            SeedDefaultApprovalsIfEmpty();
        }

        private void SeedDefaultApprovalsIfEmpty()
        {
            if (ApprovalRequests.Count > 0) return;

            ApprovalRequests = new List<ApprovalRequest>
            {
                new ApprovalRequest
                {
                    RequestId = 1,
                    CompanyId = ActiveCompanyId,
                    RequestNumber = "REQ-20260919-01",
                    RequestType = "VoidTransaction",
                    Title = "Void POS Order #ORD-1082 (Customer Double Swipe)",
                    ReasonDescription = "Customer card reader timed out during payment; second attempt succeeded but duplicate sales order recorded.",
                    RequestedBy = "Camille Dizon",
                    RequestedAmount = 18500.00m,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new ApprovalRequest
                {
                    RequestId = 2,
                    CompanyId = ActiveCompanyId,
                    RequestNumber = "REQ-20260919-02",
                    RequestType = "CustomDiscount",
                    Title = "Corporate Bulk Discount 15% - University Lab",
                    ReasonDescription = "Bulk procurement of 5x RTX 4070 GPUs for engineering computer lab.",
                    RequestedBy = "Alex Rodriguez",
                    RequestedAmount = 24750.00m,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddHours(-5)
                },
                new ApprovalRequest
                {
                    RequestId = 3,
                    CompanyId = ActiveCompanyId,
                    RequestNumber = "REQ-20260919-03",
                    RequestType = "InventoryWriteOff",
                    Title = "Damaged Packaging - Corsair RM850e PSU",
                    ReasonDescription = "Heavy monsoon delivery damaged external carton; unit fully functional but cannot be sold as new MSRP.",
                    RequestedBy = "Justin Morales",
                    RequestedAmount = 6200.00m,
                    Status = "Approved",
                    ReviewedBy = "Marcus Vance",
                    ReviewNotes = "Approved. Re-label as 25% off open-box clearance stock.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ResolvedAt = DateTime.UtcNow.AddHours(-8)
                },
                new ApprovalRequest
                {
                    RequestId = 4,
                    CompanyId = ActiveCompanyId,
                    RequestNumber = "REQ-20260919-04",
                    RequestType = "WarrantyOverride",
                    Title = "Late Warranty RMA - ASUS B650 Motherboard",
                    ReasonDescription = "Customer 3 days past 30-day store warranty; verified customer was hospitalized during warranty window.",
                    RequestedBy = "Alex Rodriguez",
                    RequestedAmount = 1200.00m,
                    Status = "Approved",
                    ReviewedBy = "Cirunay",
                    ReviewNotes = "Approved as goodwill customer retention exception.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            SaveApprovalsToLocalCache();
        }

        public void AddApprovalRequest(ApprovalRequest request)
        {
            if (request == null) return;
            request.RequestId = (ApprovalRequests.Count > 0 ? ApprovalRequests.Max(r => r.RequestId) : 0) + 1;
            request.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(request.RequestNumber))
            {
                request.RequestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "Pending";

            ApprovalRequests.Insert(0, request);
            SaveApprovalsToLocalCache();
            ApprovalRequestsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.CreateApprovalRequestAsync(ActiveCompanyId, request));
            }
        }

        public void ResolveApprovalRequest(int requestId, string status, string reviewer, string? notes)
        {
            var request = ApprovalRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null) return;

            request.Status = status;
            request.ReviewedBy = reviewer;
            request.ReviewNotes = notes;
            request.ResolvedAt = DateTime.UtcNow;

            SaveApprovalsToLocalCache();
            ApprovalRequestsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.ResolveApprovalRequestAsync(ActiveCompanyId, requestId, status, reviewer, notes));
            }
        }

        // =========================================================================
        // TENANT B: CUSTOMER MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalCustomersFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_customers.json");
        }

        public void SaveCustomersToLocalCache()
        {
            try
            {
                string path = GetLocalCustomersFilePath();
                string json = JsonSerializer.Serialize(Customers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCustomersToLocalCache error: {ex.Message}");
            }
        }

        public void LoadCustomersFromLocalCache()
        {
            try
            {
                string path = GetLocalCustomersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Customer>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Customers = cached.Where(c => c.IsActive).ToList();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCustomersFromLocalCache error: {ex.Message}");
            }

            SeedDefaultCustomersIfEmpty();
        }

        private void SeedDefaultCustomersIfEmpty()
        {
            if (Customers.Count > 0) return;

            Customers = new List<Customer>
            {
                new Customer
                {
                    CustomerId = 1,
                    CustomerCode = "CUST-1001",
                    CustomerName = "Marco Valderrama",
                    ContactNumber = "0917-555-8912",
                    EmailAddress = "marco.v@gmail.com",
                    Address = "Kapitolyo, Pasig City",
                    IsActive = true,
                    TotalOrders = 3,
                    TotalSpent = 142500.00m
                },
                new Customer
                {
                    CustomerId = 2,
                    CustomerCode = "CUST-1002",
                    CustomerName = "Sarah Jenkins",
                    ContactNumber = "0928-771-3344",
                    EmailAddress = "sarah.j@techhub.ph",
                    Address = "Ortigas Center, Pasig City",
                    IsActive = true,
                    TotalOrders = 2,
                    TotalSpent = 78900.00m
                },
                new Customer
                {
                    CustomerId = 3,
                    CustomerCode = "CUST-1003",
                    CustomerName = "David Lim",
                    ContactNumber = "0918-333-7890",
                    EmailAddress = "david.lim@outlook.com",
                    Address = "Greenhills, San Juan",
                    IsActive = true,
                    TotalOrders = 5,
                    TotalSpent = 310200.00m
                },
                new Customer
                {
                    CustomerId = 4,
                    CustomerCode = "CUST-1004",
                    CustomerName = "Patricia Santos",
                    ContactNumber = "0905-224-8899",
                    EmailAddress = "patricia.s@bpo-center.com",
                    Address = "Eastwood City, Quezon City",
                    IsActive = true,
                    TotalOrders = 1,
                    TotalSpent = 34500.00m
                },
                new Customer
                {
                    CustomerId = 5,
                    CustomerCode = "CUST-1005",
                    CustomerName = "Ateneo Robotics Lab",
                    ContactNumber = "0919-888-4422",
                    EmailAddress = "robotics@ateneo.edu",
                    Address = "Loyola Heights, Quezon City",
                    IsActive = true,
                    TotalOrders = 4,
                    TotalSpent = 265000.00m
                }
            };

            SaveCustomersToLocalCache();
        }

        public void AddCustomer(Customer customer)
        {
            if (customer == null) return;
            customer.CustomerId = (Customers.Count > 0 ? Customers.Max(c => c.CustomerId) : 0) + 1;
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = $"CUST-{new Random().Next(1000, 9999)}";
            }
            customer.CreatedAt = DateTime.UtcNow;
            customer.IsActive = true;

            Customers.Add(customer);
            SaveCustomersToLocalCache();
            CustomersChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.CreateCustomerAsync(ActiveCompanyId, customer));
            }
        }

        public void UpdateCustomer(Customer customer)
        {
            if (customer == null) return;
            var existing = Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (existing != null)
            {
                existing.CustomerName = customer.CustomerName;
                existing.ContactNumber = customer.ContactNumber;
                existing.EmailAddress = customer.EmailAddress;
                existing.Address = customer.Address;

                SaveCustomersToLocalCache();
                CustomersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.UpdateCustomerAsync(ActiveCompanyId, customer.CustomerId, existing));
                }
            }
        }

        public void DeleteCustomer(int customerId)
        {
            var existing = Customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (existing != null)
            {
                existing.IsActive = false;
                Customers.Remove(existing);
                SaveCustomersToLocalCache();
                CustomersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.DeleteCustomerAsync(ActiveCompanyId, customerId));
                }
            }
        }

        public decimal GetTotalRevenue() => Orders.Sum(o => o.TotalAmount);
        public int GetTotalOrders() => Orders.Count;
        public int GetTotalProductsCount() => Products.Count;
        public int GetLowStockCount() => Products.Count(p => p.IsLowStock);

        // =========================================================================
        // TENANT B: STORE PAYROLL CALCULATOR CACHING & CRUD
        // =========================================================================
        private string GetLocalPayrollFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_payroll.json");
        }

        public void SavePayrollToLocalCache()
        {
            try
            {
                string path = GetLocalPayrollFilePath();
                string json = JsonSerializer.Serialize(PayrollRecords, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SavePayrollToLocalCache error: {ex.Message}");
            }
        }

        public void LoadPayrollFromLocalCache()
        {
            try
            {
                string path = GetLocalPayrollFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<PayrollRecord>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        PayrollRecords = cached.OrderByDescending(p => p.ProcessedAt).ToList();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPayrollFromLocalCache error: {ex.Message}");
            }

            SeedDefaultPayrollIfEmpty();
        }

        private void SeedDefaultPayrollIfEmpty()
        {
            if (PayrollRecords.Count > 0) return;

            var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var periodEnd = periodStart.AddDays(14);

            PayrollRecords = new List<PayrollRecord>
            {
                new PayrollRecord
                {
                    PayrollId = 1,
                    CompanyId = ActiveCompanyId,
                    StaffId = 1,
                    StaffName = "Marcus Vance",
                    Role = "Store Manager",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    BaseSalary = 22500.00m,
                    OvertimePay = 0.00m,
                    CommissionAmount = 4500.00m,
                    Deductions = 2100.00m,
                    Status = "Paid",
                    PaymentMethod = "Bank Transfer (BDO)",
                    ProcessedAt = DateTime.UtcNow.AddDays(-3),
                    ProcessedBy = "Admin (Cirunay)"
                },
                new PayrollRecord
                {
                    PayrollId = 2,
                    CompanyId = ActiveCompanyId,
                    StaffId = 2,
                    StaffName = "Alex Rodriguez",
                    Role = "Lead Bench Technician",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    BaseSalary = 16000.00m,
                    OvertimePay = 2400.00m,
                    CommissionAmount = 3500.00m,
                    Deductions = 1450.00m,
                    Status = "Paid",
                    PaymentMethod = "Bank Transfer (BPI)",
                    ProcessedAt = DateTime.UtcNow.AddDays(-3),
                    ProcessedBy = "Marcus Vance"
                },
                new PayrollRecord
                {
                    PayrollId = 3,
                    CompanyId = ActiveCompanyId,
                    StaffId = 3,
                    StaffName = "Justin Morales",
                    Role = "Junior Hardware Specialist",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    BaseSalary = 11000.00m,
                    OvertimePay = 1350.00m,
                    CommissionAmount = 1800.00m,
                    Deductions = 950.00m,
                    Status = "Paid",
                    PaymentMethod = "Bank Transfer (BPI)",
                    ProcessedAt = DateTime.UtcNow.AddDays(-3),
                    ProcessedBy = "Marcus Vance"
                },
                new PayrollRecord
                {
                    PayrollId = 4,
                    CompanyId = ActiveCompanyId,
                    StaffId = 4,
                    StaffName = "Sarah Jenkins",
                    Role = "Sales & Counter Specialist",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    BaseSalary = 10000.00m,
                    OvertimePay = 800.00m,
                    CommissionAmount = 5200.00m,
                    Deductions = 900.00m,
                    Status = "Paid",
                    PaymentMethod = "Cash Counter",
                    ProcessedAt = DateTime.UtcNow.AddDays(-3),
                    ProcessedBy = "Marcus Vance"
                },
                new PayrollRecord
                {
                    PayrollId = 5,
                    CompanyId = ActiveCompanyId,
                    StaffId = 5,
                    StaffName = "David Lim",
                    Role = "Inventory Associate",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    BaseSalary = 9500.00m,
                    OvertimePay = 950.00m,
                    CommissionAmount = 750.00m,
                    Deductions = 850.00m,
                    Status = "Paid",
                    PaymentMethod = "Bank Transfer (Metrobank)",
                    ProcessedAt = DateTime.UtcNow.AddDays(-3),
                    ProcessedBy = "Marcus Vance"
                }
            };

            SavePayrollToLocalCache();
        }

        public void AddPayrollRecord(PayrollRecord record)
        {
            if (record == null) return;
            record.PayrollId = (PayrollRecords.Count > 0 ? PayrollRecords.Max(p => p.PayrollId) : 0) + 1;
            record.CompanyId = ActiveCompanyId;
            record.ProcessedAt = DateTime.UtcNow;

            PayrollRecords.Insert(0, record);
            SavePayrollToLocalCache();
            PayrollRecordsChanged?.Invoke();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(() => _apiClient.CreatePayrollRecordAsync(ActiveCompanyId, record));
            }
        }

        public void DeletePayrollRecord(int payrollId)
        {
            var record = PayrollRecords.FirstOrDefault(p => p.PayrollId == payrollId);
            if (record != null)
            {
                PayrollRecords.Remove(record);
                SavePayrollToLocalCache();
                PayrollRecordsChanged?.Invoke();
            }
        }

        // =========================================================================
        // TENANT B: TERMS, POLICIES & AGREEMENTS CACHING & CRUD
        // =========================================================================
        private string GetLocalPoliciesFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_policies.json");
        }

        public void SavePoliciesToLocalCache()
        {
            try
            {
                string path = GetLocalPoliciesFilePath();
                string json = JsonSerializer.Serialize(StorePolicies, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SavePoliciesToLocalCache error: {ex.Message}");
            }
        }

        public void LoadPoliciesFromLocalCache()
        {
            try
            {
                string path = GetLocalPoliciesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<StorePolicy>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        StorePolicies = cached;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPoliciesFromLocalCache error: {ex.Message}");
            }

            SeedDefaultPoliciesIfEmpty();
        }

        private void SeedDefaultPoliciesIfEmpty()
        {
            if (StorePolicies.Count > 0) return;

            StorePolicies = new List<StorePolicy>
            {
                new StorePolicy
                {
                    PolicyId = 1,
                    CompanyId = ActiveCompanyId,
                    PolicyType = "RepairLiabilityWaiver",
                    Title = "Service & Repair Diagnostic Waiver",
                    ContentText = @"Morphic Computers & IT8 TechStore Service & Repair Bench Terms:

1. DATA LIABILITY & BACKUP MANDATE:
The customer acknowledges and accepts sole responsibility for backing up all personal documents, files, operating systems, and proprietary software prior to submitting equipment for diagnostic or hardware repair. IT8 TechStore / Morphic Computers and its bench technicians assume no liability whatsoever for partial or total data loss, corrupt storage sectors, or drive formatting.

2. UNCLAIMED HARDWARE DISPOSAL:
Any repaired, diagnosed, or abandoned device left unclaimed past ninety (90) calendar days from the official completion notification date will be considered legally abandoned. IT8 TechStore reserves the right to dispose of, scrap, or liquidate the unit to recover unpaid bench labor and storage expenses.

3. PRE-EXISTING DAMAGE & DIAGNOSTIC SCOPE:
Bench technicians conduct an intake exterior inspection. Prior physical drops, liquid exposure, burnt traces, or unauthorized third-party repairs void standard warranty eligibility.

4. MINIMUM DIAGNOSTIC CHARGE:
A minimum bench assessment fee of PHP 500.00 applies to all hardware diagnostic requests if the customer subsequently declines the quoted repair service estimate.",
                    LastUpdatedBy = "Admin (Cirunay)",
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new StorePolicy
                {
                    PolicyId = 2,
                    CompanyId = ActiveCompanyId,
                    PolicyType = "30DayWarrantyTerms",
                    Title = "30-Day Hardware Replacement & Warranty Policy",
                    ContentText = @"Store Hardware Warranty & Manufacturer Terms:

1. 30-DAY DIRECT STORE REPLACEMENT:
All brand-new desktop components, GPUs, CPUs, motherboards, RAM kits, power supplies, and solid-state drives purchased from Morphic Computers include a 30-day direct replacement warranty against factory defects from the date of sales invoice.

2. MANUFACTURER PASSTHROUGH WARRANTY:
After the initial 30-day store warranty window, equipment remains covered under the respective authorized manufacturer distributor warranty (1 to 3 years depending on vendor). IT8 TechStore assists in forwarding RMA units to local service centers.

3. WARRANTY EXCLUSIONS & VOID CONDITIONS:
Warranty does not cover:
- Physical damage, fractured PCB, bent socket pins, or cracked heatsinks.
- Electrical surge burn, lightning strikes, or improper power supply usage.
- Firmware corruption resulting from unauthorized BIOS / vBIOS flashing.
- Removal or tampering of serial number barcodes and tamper seals.

4. MANDATORY INVOICE & COMPLETE PACKAGING:
Original official receipt / invoice and original packaging (including boxes, manuals, and accessories) are strictly required for warranty verification.",
                    LastUpdatedBy = "Admin (Cirunay)",
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new StorePolicy
                {
                    PolicyId = 3,
                    CompanyId = ActiveCompanyId,
                    PolicyType = "ReturnAndRefundPolicy",
                    Title = "Customer Returns, Exchanges & Refund Terms",
                    ContentText = @"Consumer Return & Refund Guidelines:

1. DEFECTIVE PRODUCTS:
Merchandise verified defective upon unboxing within seven (7) calendar days of purchase qualifies for immediate 1-to-1 replacement with identical stock or full refund.

2. CHANGE-OF-MIND RETURNS:
In accordance with Republic Act No. 7394 (Consumer Act of the Philippines), change-of-mind requests are subject to store approval. If approved, items must be unopened in mint condition and are subject to a 10% restocking and handling charge.

3. DIGITAL KEYS & SOFTWARE:
Opened software packaging, digital license keys, Windows OS OEM activations, and Microsoft Office vouchers are non-returnable and non-refundable once scratched, revealed, or registered.

4. REFUND SETTLEMENT:
Approved refunds are processed through the original method of payment (Cash at counter, or 3-7 banking days for credit card / GCash merchant reversals).",
                    LastUpdatedBy = "Admin (Cirunay)",
                    UpdatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new StorePolicy
                {
                    PolicyId = 4,
                    CompanyId = ActiveCompanyId,
                    PolicyType = "DataPrivacyNotice",
                    Title = "Data Privacy & Confidentiality Notice",
                    ContentText = @"Data Privacy Notice (Republic Act No. 10173):

1. COLLECTION OF PERSONAL DATA:
Morphic Computers / IT8 TechStore collects customer contact details (Full Name, Phone Number, Email, and Delivery Address) exclusively for transaction processing, warranty verification, repair tracking stubs, and official receipt issuance.

2. SECURITY & RETENTION:
Personal data is securely encrypted in our enterprise multi-tenant database. Access is strictly restricted to authorized staff, managers, and administrators.

3. THIRD-PARTY DISCLOSURE:
We do not sell, rent, or trade customer contact details to third-party advertisers. Data is shared with courier services solely for delivery fulfillment.

4. INQUIRIES & DELETION REQUESTS:
Customers may request a copy or deletion of their contact profile at any time by contacting our store management desk.",
                    LastUpdatedBy = "Admin (Cirunay)",
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                }
            };

            SavePoliciesToLocalCache();
        }

        public StorePolicy? GetPolicy(string policyType)
        {
            return StorePolicies.FirstOrDefault(p => string.Equals(p.PolicyType, policyType, StringComparison.OrdinalIgnoreCase));
        }

        public void UpdateStorePolicy(string policyType, string content, string updatedBy)
        {
            var policy = StorePolicies.FirstOrDefault(p => string.Equals(p.PolicyType, policyType, StringComparison.OrdinalIgnoreCase));
            if (policy != null)
            {
                policy.ContentText = content;
                policy.LastUpdatedBy = updatedBy;
                policy.UpdatedAt = DateTime.UtcNow;

                SavePoliciesToLocalCache();
                StorePoliciesChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.UpdatePolicyAsync(ActiveCompanyId, policyType, content, updatedBy));
                }
            }
        }

        #region Business Intelligence Analytics
        public List<Order> GetOrdersForTimeRange(BiTimeRange range)
        {
            var now = DateTime.Now;
            return range switch
            {
                BiTimeRange.Today => Orders.Where(o => o.CreatedAt.Date == now.Date).ToList(),
                BiTimeRange.ThisWeek => Orders.Where(o => o.CreatedAt.Date >= now.Date.AddDays(-6)).ToList(),
                BiTimeRange.ThisMonth => Orders.Where(o => o.CreatedAt.Year == now.Year && o.CreatedAt.Month == now.Month).ToList(),
                _ => Orders.ToList()
            };
        }

        public BiSalesMetrics GetBiSalesMetrics(BiTimeRange range)
        {
            var orders = GetOrdersForTimeRange(range);
            decimal revenue = orders.Sum(o => o.TotalAmount);
            int count = orders.Count;
            int itemsSold = orders.SelectMany(o => o.Items).Sum(i => i.Quantity);

            // Estimate profit margin at ~28.5% typical for PC hardware retail
            decimal grossProfit = revenue * 0.285m;

            // Calculate growth compared to prior equivalent period
            decimal priorRevenue = 0m;
            var now = DateTime.Now;
            if (range == BiTimeRange.Today)
            {
                priorRevenue = Orders.Where(o => o.CreatedAt.Date == now.Date.AddDays(-1)).Sum(o => o.TotalAmount);
            }
            else if (range == BiTimeRange.ThisWeek)
            {
                priorRevenue = Orders.Where(o => o.CreatedAt.Date >= now.Date.AddDays(-13) && o.CreatedAt.Date < now.Date.AddDays(-6)).Sum(o => o.TotalAmount);
            }
            else if (range == BiTimeRange.ThisMonth)
            {
                var prevMonth = now.AddMonths(-1);
                priorRevenue = Orders.Where(o => o.CreatedAt.Year == prevMonth.Year && o.CreatedAt.Month == prevMonth.Month).Sum(o => o.TotalAmount);
            }

            decimal growthRate = 0m;
            if (priorRevenue > 0)
            {
                growthRate = ((revenue - priorRevenue) / priorRevenue) * 100m;
            }
            else if (revenue > 0)
            {
                growthRate = 100m;
            }

            return new BiSalesMetrics
            {
                TotalRevenue = revenue,
                OrderCount = count,
                TotalItemsSold = itemsSold,
                GrossProfit = grossProfit,
                GrowthRate = growthRate
            };
        }

        public List<BiCategoryShare> GetBiCategoryDistribution(BiTimeRange range)
        {
            var orders = GetOrdersForTimeRange(range);
            var categoryMap = Products.ToDictionary(p => p.ProductId, p => p.CategoryName);

            var list = new Dictionary<string, (decimal Revenue, int Units)>(StringComparer.OrdinalIgnoreCase);

            foreach (var o in orders)
            {
                foreach (var item in o.Items)
                {
                    string cat = "Peripherals";
                    if (categoryMap.TryGetValue(item.ProductId, out var mappedCat) && !string.IsNullOrWhiteSpace(mappedCat))
                    {
                        cat = mappedCat;
                    }
                    else
                    {
                        var prod = Products.FirstOrDefault(p => p.Name.Equals(item.ProductName, StringComparison.OrdinalIgnoreCase));
                        if (prod != null && !string.IsNullOrWhiteSpace(prod.CategoryName))
                        {
                            cat = prod.CategoryName;
                        }
                    }

                    cat = cat.Trim();
                    (decimal Revenue, int Units) cur = list.TryGetValue(cat, out var existing) ? existing : (0m, 0);
                    list[cat] = (cur.Revenue + item.TotalPrice, cur.Units + item.Quantity);
                }
            }

            decimal totalRevenue = list.Values.Sum(v => v.Revenue);
            if (totalRevenue == 0m) totalRevenue = 1m;

            return list
                .Select(kvp => new BiCategoryShare
                {
                    CategoryName = kvp.Key,
                    Revenue = kvp.Value.Revenue,
                    UnitsSold = kvp.Value.Units,
                    Percentage = Math.Round((kvp.Value.Revenue / totalRevenue) * 100m, 1)
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();
        }

        public List<BiProductPerformance> GetBiBestSellers(BiTimeRange range, int topN = 5)
        {
            var orders = GetOrdersForTimeRange(range);
            var grouped = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId > 0 ? i.ProductId.ToString() : i.ProductName)
                .Select(g =>
                {
                    var first = g.First();
                    var prod = Products.FirstOrDefault(p => p.ProductId == first.ProductId || p.Name.Equals(first.ProductName, StringComparison.OrdinalIgnoreCase));
                    decimal rev = g.Sum(x => x.TotalPrice);
                    int qty = g.Sum(x => x.Quantity);
                    return new BiProductPerformance
                    {
                        ProductId = prod?.ProductId ?? first.ProductId,
                        ProductName = prod?.Name ?? first.ProductName,
                        CategoryName = prod?.CategoryName ?? "Hardware",
                        Revenue = rev,
                        UnitsSold = qty,
                        CurrentStock = prod?.StockQuantity ?? 0,
                        UnitPrice = prod?.Price ?? first.UnitPrice
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .Take(topN)
                .ToList();

            return grouped;
        }

        public BiStockTrends GetBiStockTrends()
        {
            int totalItems = Products.Count;
            int totalStock = Products.Sum(p => p.StockQuantity);
            decimal valuation = Products.Sum(p => p.StockQuantity * p.Price);

            int healthy = Products.Count(p => p.StockQuantity > 5);
            int low = Products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
            int outOfStock = Products.Count(p => p.StockQuantity == 0);

            var bestSellersAll = GetBiBestSellers(BiTimeRange.AllTime, 5);
            var slowMoving = Products
                .Where(p => p.StockQuantity > 5 && !bestSellersAll.Any(b => b.ProductId == p.ProductId))
                .Take(5)
                .ToList();

            return new BiStockTrends
            {
                TotalCatalogItems = totalItems,
                TotalStockUnits = totalStock,
                TotalAssetValuation = valuation,
                HealthyStockCount = healthy,
                LowStockCount = low,
                OutOfStockCount = outOfStock,
                FastMovingProducts = bestSellersAll,
                SlowMovingProducts = slowMoving
            };
        }

        public BiEarningsSummary GetBiEarningsSummary()
        {
            var now = DateTime.Now;
            var monthOrders = Orders.Where(o => o.CreatedAt.Year == now.Year && o.CreatedAt.Month == now.Month).ToList();
            decimal grossRevenue = monthOrders.Sum(o => o.TotalAmount);
            decimal estimatedCogs = grossRevenue * 0.715m;
            decimal grossProfit = grossRevenue - estimatedCogs;
            decimal marginPercent = grossRevenue > 0 ? (grossProfit / grossRevenue) * 100m : 28.5m;

            int dayOfMonth = Math.Max(1, now.Day);
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            decimal dailyRunRate = grossRevenue / dayOfMonth;
            decimal projected = dailyRunRate * daysInMonth;

            return new BiEarningsSummary
            {
                GrossRevenue = grossRevenue,
                EstimatedCogs = estimatedCogs,
                GrossProfit = grossProfit,
                MarginPercent = marginPercent,
                ProjectedMonthEnd = projected,
                TotalTransactions = monthOrders.Count
            };
        }
        #endregion
    }

    public enum BiTimeRange
    {
        Today,
        ThisWeek,
        ThisMonth,
        AllTime
    }

    public class BiSalesMetrics
    {
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue => OrderCount > 0 ? TotalRevenue / OrderCount : 0m;
        public int TotalItemsSold { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin => TotalRevenue > 0 ? (GrossProfit / TotalRevenue) * 100m : 0m;
    }

    public class BiCategoryShare
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
        public decimal Percentage { get; set; }
    }

    public class BiProductPerformance
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
        public int CurrentStock { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class BiStockTrends
    {
        public int TotalCatalogItems { get; set; }
        public int TotalStockUnits { get; set; }
        public decimal TotalAssetValuation { get; set; }
        public int HealthyStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal InStockRate => TotalCatalogItems > 0 ? (decimal)(HealthyStockCount + LowStockCount) / TotalCatalogItems * 100m : 100m;
        public List<BiProductPerformance> FastMovingProducts { get; set; } = new();
        public List<Product> SlowMovingProducts { get; set; } = new();
    }

    public class BiEarningsSummary
    {
        public decimal GrossRevenue { get; set; }
        public decimal EstimatedCogs { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal ProjectedMonthEnd { get; set; }
        public int TotalTransactions { get; set; }
    }
}
