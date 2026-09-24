using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.domain.entities;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Data Access Service connecting ERP.winforms UI to ERP.api over HTTPS.
    /// Communicates with backend REST API using internal security authentication without exposing raw database credentials.
    /// </summary>
    public partial class DataService
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
        public List<ExpenseRecord> ExpenseRecords { get; private set; } = new();

        public Action? CategoriesChanged;
        public Action? ProductsChanged;
        public Action? RepairTicketsChanged;
        public Action? SuppliersChanged;
        public Action? StaffMembersChanged;
        public Action? ApprovalRequestsChanged;
        public Action? CustomersChanged;
        public Action? PayrollRecordsChanged;
        public Action? StorePoliciesChanged;
        public Action? ExpensesChanged;
        public Action? OrdersChanged;
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
            ExpenseRecords.Clear();

            LoadCategoriesFromLocalCache();
            LoadProductsToLocalCache();
            LoadOrdersFromLocalCache();
            LoadRepairsFromLocalCache();
            LoadSuppliersFromLocalCache();
            LoadStaffFromLocalCache();
            LoadApprovalsFromLocalCache();
            LoadCustomersFromLocalCache();
            LoadPayrollFromLocalCache();
            LoadPoliciesFromLocalCache();
            LoadExpensesFromLocalCache();

            CategoriesChanged?.Invoke();
            ProductsChanged?.Invoke();
            RepairTicketsChanged?.Invoke();
            SuppliersChanged?.Invoke();
            StaffMembersChanged?.Invoke();
            ApprovalRequestsChanged?.Invoke();
            CustomersChanged?.Invoke();
            PayrollRecordsChanged?.Invoke();
            StorePoliciesChanged?.Invoke();
            ExpensesChanged?.Invoke();

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

            // Standard categories (empty by default; loaded from API or local cache)
            Categories = new List<Category>();

            // Immediate local cache hydration: UI is instantly populated without waiting on network
            LoadCategoriesFromLocalCache();
            LoadProductsToLocalCache();
            LoadOrdersFromLocalCache();
            LoadRepairsFromLocalCache();
            LoadSuppliersFromLocalCache();
            LoadStaffFromLocalCache();
            LoadApprovalsFromLocalCache();
            LoadCustomersFromLocalCache();
            LoadPayrollFromLocalCache();
            LoadPoliciesFromLocalCache();
            LoadExpensesFromLocalCache();

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
                if (liveCategories != null && liveCategories.Count > 0)
                {
                    Categories = liveCategories;
                    SaveCategoriesToLocalCache();
                    CategoriesChanged?.Invoke();
                }

                // 2. Fetch live products for the active tenant
                var liveProducts = Task.Run(() => _apiClient.GetProductsAsync(ActiveCompanyId)).GetAwaiter().GetResult();
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
                OrdersChanged?.Invoke();
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
                if (product.ProductId > 0)
                {
                    var existing = Products.FirstOrDefault(p => p.ProductId == product.ProductId);
                    if (existing != null)
                    {
                        return UpdateProduct(product);
                    }
                }
                else
                {
                    // New product: reject if SKU already in use to prevent overwriting
                    if (Products.Any(p => p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveProduct: duplicate SKU '{product.ProductCode}' rejected.");
                        return false;
                    }
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
                    if (cached != null)
                    {
                        RepairTickets = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRepairsFromLocalCache error: {ex.Message}");
            }
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
                    if (cached != null)
                    {
                        Suppliers = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSuppliersFromLocalCache error: {ex.Message}");
            }
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

        public void ToggleSupplierArchive(int supplierId)
        {
            var existing = Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
            if (existing != null)
            {
                existing.IsActive = !existing.IsActive;
                SaveSuppliersToLocalCache();
                SuppliersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    if (!existing.IsActive)
                        Task.Run(() => _apiClient.DeleteSupplierAsync(ActiveCompanyId, supplierId));
                    else
                        Task.Run(() => _apiClient.UpdateSupplierAsync(ActiveCompanyId, supplierId, existing));
                }
            }
        }

        public void DeleteSupplier(int supplierId) => ToggleSupplierArchive(supplierId);

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
                    if (cached != null)
                    {
                        StaffMembers = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStaffFromLocalCache error: {ex.Message}");
            }
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

            if (!string.IsNullOrWhiteSpace(staff.InitialPassword))
            {
                OfflineAuthService.Instance.RegisterOrUpdateStaffPassword(
                    ActiveCompanyId,
                    CurrentCompany.CompanyCode,
                    CurrentCompany.CompanyName,
                    CurrentCompany.PlanName,
                    staff.Username,
                    staff.FullName,
                    staff.Role,
                    staff.InitialPassword
                );
            }

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

                if (!string.IsNullOrWhiteSpace(staff.InitialPassword))
                {
                    existing.InitialPassword = staff.InitialPassword;
                    OfflineAuthService.Instance.RegisterOrUpdateStaffPassword(
                        ActiveCompanyId,
                        CurrentCompany.CompanyCode,
                        CurrentCompany.CompanyName,
                        CurrentCompany.PlanName,
                        existing.Username,
                        existing.FullName,
                        existing.Role,
                        staff.InitialPassword
                    );
                }

                SaveStaffToLocalCache();
                StaffMembersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Task.Run(() => _apiClient.UpdateStaffAsync(ActiveCompanyId, staff.StaffId, existing));
                }
            }
        }

        public void ToggleStaffArchive(int staffId)
        {
            var existing = StaffMembers.FirstOrDefault(s => s.StaffId == staffId);
            if (existing != null)
            {
                existing.IsActive = !existing.IsActive;
                SaveStaffToLocalCache();
                StaffMembersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    if (!existing.IsActive)
                        Task.Run(() => _apiClient.DeactivateStaffAsync(ActiveCompanyId, staffId));
                    else
                        Task.Run(() => _apiClient.UpdateStaffAsync(ActiveCompanyId, staffId, existing));
                }
            }
        }

        public void DeactivateStaffMember(int staffId) => ToggleStaffArchive(staffId);

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
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadApprovalsFromLocalCache error: {ex.Message}");
            }
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
                        foreach (var c in cached)
                        {
                            if (string.IsNullOrWhiteSpace(c.Address) || !c.Address.Contains("Davao", StringComparison.OrdinalIgnoreCase))
                            {
                                c.Address = c.CustomerId switch
                                {
                                    1 => "J.P. Laurel Ave, Bajada, Davao City",
                                    2 => "McArthur Highway, Matina, Davao City",
                                    3 => "Lanang Business Park, Lanang, Davao City",
                                    4 => "Quimpo Blvd, Ecoland, Davao City",
                                    5 => "Roxas Ave, Poblacion District, Davao City",
                                    _ => string.IsNullOrWhiteSpace(c.Address) ? "Poblacion District, Davao City" : $"{c.Address}, Davao City"
                                };
                            }
                        }
                        Customers = cached;
                        SaveCustomersToLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCustomersFromLocalCache error: {ex.Message}");
            }
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

        public void ToggleCustomerArchive(int customerId)
        {
            var existing = Customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (existing != null)
            {
                existing.IsActive = !existing.IsActive;
                SaveCustomersToLocalCache();
                CustomersChanged?.Invoke();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    if (!existing.IsActive)
                        Task.Run(() => _apiClient.DeleteCustomerAsync(ActiveCompanyId, customerId));
                    else
                        Task.Run(() => _apiClient.UpdateCustomerAsync(ActiveCompanyId, customerId, existing));
                }
            }
        }

        public void DeleteCustomer(int customerId) => ToggleCustomerArchive(customerId);

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
                    if (cached != null)
                    {
                        PayrollRecords = cached.OrderByDescending(p => p.ProcessedAt).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPayrollFromLocalCache error: {ex.Message}");
            }
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
                    if (cached != null)
                    {
                        StorePolicies = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPoliciesFromLocalCache error: {ex.Message}");
            }
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

    public partial class DataService
    {
        // =========================================================================
        // TENANT B: FINANCE & ACCOUNTING / EXPENSES CACHING & CRUD
        // =========================================================================
        private string GetLocalExpensesFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_expenses.json");
        }

        public void SaveExpensesToLocalCache()
        {
            try
            {
                string path = GetLocalExpensesFilePath();
                string json = JsonSerializer.Serialize(ExpenseRecords, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveExpensesToLocalCache error: {ex.Message}");
            }
        }

        public void LoadExpensesFromLocalCache()
        {
            try
            {
                string path = GetLocalExpensesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<ExpenseRecord>>(json);
                    if (cached != null)
                    {
                        ExpenseRecords = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadExpensesFromLocalCache error: {ex.Message}");
            }
        }

        public void AddExpense(ExpenseRecord expense)
        {
            if (expense == null) return;
            expense.ExpenseId = (ExpenseRecords.Count > 0 ? ExpenseRecords.Max(e => e.ExpenseId) : 0) + 1;
            expense.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(expense.ExpenseNumber))
            {
                expense.ExpenseNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            if (expense.ExpenseDate == default)
            {
                expense.ExpenseDate = DateTime.UtcNow;
            }

            ExpenseRecords.Insert(0, expense);
            SaveExpensesToLocalCache();
            ExpensesChanged?.Invoke();
        }

        public void ToggleExpenseArchive(int expenseId)
        {
            var exp = ExpenseRecords.FirstOrDefault(e => e.ExpenseId == expenseId);
            if (exp != null)
            {
                exp.IsActive = !exp.IsActive;
                SaveExpensesToLocalCache();
                ExpensesChanged?.Invoke();
            }
        }

        public void DeleteExpense(int expenseId)
        {
            ToggleExpenseArchive(expenseId);
        }

        public decimal GetTotalRetailSalesRevenue()
        {
            return Orders.Where(o => !string.Equals(o.Status, "Voided", StringComparison.OrdinalIgnoreCase)).Sum(o => o.TotalAmount);
        }

        public decimal GetTotalRepairServicesRevenue()
        {
            return RepairTickets.Where(t => string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Status, "Released", StringComparison.OrdinalIgnoreCase)).Sum(t => t.TotalAmount);
        }

        public decimal GetTotalPayrollExpense()
        {
            return PayrollRecords.Sum(p => p.NetPay);
        }

        public decimal GetTotalOperatingExpenses()
        {
            return ExpenseRecords.Where(e => e.IsActive).Sum(e => e.Amount);
        }

        public decimal GetTotalVatCollected()
        {
            return Orders.Where(o => !string.Equals(o.Status, "Voided", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Tax);
        }
    }
}
