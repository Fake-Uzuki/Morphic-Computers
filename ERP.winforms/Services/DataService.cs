using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        public int ActiveCompanyId { get; set; } = 1;
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
            LoadOrdersFromLocalCache();

            // Background / live cloud refresh
            LoadFromDatabase();
        }

        public void LoadFromDatabase()
        {
            // Ensure local cache is in memory immediately
            if (Categories.Count == 0) LoadCategoriesFromLocalCache();
            if (Products.Count == 0) LoadProductsToLocalCache();
            if (Orders.Count == 0) LoadOrdersFromLocalCache();

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
                }
                CategoriesChanged?.Invoke();

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
            if (Orders.Count == 0) LoadOrdersFromLocalCache();
            IsUsingLiveCloudDatabase = false;
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
        }

        private static string GetTenantConnectionString(int companyId)
        {
            return companyId == 2
                ? "Server=db66562.public.databaseasp.net;Database=db66562;User Id=db66562;Password=Ex6_n9#YZb3%;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15;"
                : "Server=db67673.public.databaseasp.net;Database=db67673;User Id=db67673;Password=Wt7-8=mFA3#i;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15;";
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

                // Send to ERP.api safely off UI thread
                var created = Task.Run(() => _apiClient.AddProductAsync(ActiveCompanyId, product)).GetAwaiter().GetResult();
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
            }

            try
            {
                bool synced = Task.Run(() => _apiClient.UpdateProductAsync(ActiveCompanyId, product)).GetAwaiter().GetResult();
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

            return true;
        }

        public bool DeleteProduct(int productId)
        {
            var p = Products.FirstOrDefault(x => x.ProductId == productId);
            if (p != null) Products.Remove(p);

            try
            {
                Task.Run(() => _apiClient.DeleteProductAsync(ActiveCompanyId, productId)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteProduct API error: {ex.Message}");
            }

            return true;
        }

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

            // Save immediately to local persistent cache so transactions are never lost
            SaveOrdersToLocalCache();

            // Sync with ERP.api / MonsterASP DB safely off UI thread
            try
            {
                bool synced = Task.Run(() => _apiClient.ProcessOrderAsync(ActiveCompanyId, order)).GetAwaiter().GetResult();
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
