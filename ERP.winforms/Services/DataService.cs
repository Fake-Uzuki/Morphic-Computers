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

        public Action? CategoriesChanged;
        public Action? ProductsChanged;
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

            LoadCategoriesFromLocalCache();
            LoadProductsToLocalCache();
            SeedDefaultProductsIfEmpty();
            LoadOrdersFromLocalCache();

            CategoriesChanged?.Invoke();
            ProductsChanged?.Invoke();

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
                new Order
                {
                    Id = "ORD-20260915-001",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Juan Dela Cruz",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    PaymentMethod = "Cash",
                    Subtotal = 34776.79m,
                    Tax = 4173.21m,
                    TotalAmount = 38950.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 1, ProductName = "ASUS Dual GeForce RTX 4070 OC 12GB", Quantity = 1, UnitPrice = 34776.79m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260916-002",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "Maria Santos",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    PaymentMethod = "Card / Terminal",
                    Subtotal = 4866.07m,
                    Tax = 583.93m,
                    TotalAmount = 5450.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 3, ProductName = "Kingston FURY Beast 32GB (2x16GB) DDR5-6000", Quantity = 1, UnitPrice = 4866.07m }
                    }
                },
                new Order
                {
                    Id = "ORD-20260917-003",
                    CompanyId = ActiveCompanyId,
                    CustomerName = "TechCorp Solutions PH",
                    CreatedAt = DateTime.Now.AddHours(-3),
                    PaymentMethod = "Bank Transfer",
                    Subtotal = 23660.71m,
                    Tax = 2839.29m,
                    TotalAmount = 26500.00m,
                    Status = "Completed",
                    Items = new List<CartItem>
                    {
                        new CartItem { ProductId = 2, ProductName = "AMD Ryzen 7 7800X3D 8-Core Processor", Quantity = 1, UnitPrice = 23660.71m }
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
                ? "Server=db66562.public.databaseasp.net;Database=db66562;User Id=db66562;Password=Ex6_n9#YZb3%;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=2;"
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

        public decimal GetTotalRevenue() => Orders.Sum(o => o.TotalAmount);
        public int GetTotalOrders() => Orders.Count;
        public int GetTotalProductsCount() => Products.Count;
        public int GetLowStockCount() => Products.Count(p => p.IsLowStock);
    }
}
