using System;
using System.Collections.Generic;
using System.Linq;
using IT8_TechStore.Database;
using IT8_TechStore.Models;

namespace IT8_TechStore.Services
{
    /// <summary>
    /// Central Data Access Service utilizing Entity Framework Core (EF Core) DbContext
    /// and supporting Multi-Tenant company routing (Company A, Company B, Company C).
    /// </summary>
    public class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        public List<CompanyTenant> Tenants { get; private set; } = new();
        public List<Product> Products { get; private set; } = new();
        public List<Category> Categories { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();

        public string ActiveTenantCode { get; set; } = "COMPANY_A";
        public bool IsUsingSsmsDatabase { get; private set; }

        private DataService()
        {
            InitializeDataStore();
        }

        private void InitializeDataStore()
        {
            IsUsingSsmsDatabase = DbInitializer.InitializeDatabase();

            if (IsUsingSsmsDatabase)
            {
                LoadFromDatabase();
            }
            else
            {
                LoadCleanCategories();
            }
        }

        public void LoadFromDatabase()
        {
            try
            {
                using var db = new MorphicDbContext();
                Tenants = db.Tenants.Where(t => t.IsActive).ToList();
                Categories = db.Categories.Where(c => c.TenantCode == ActiveTenantCode || c.TenantCode == "COMPANY_A").ToList();
                Products = db.Products.Where(p => p.TenantCode == ActiveTenantCode || p.TenantCode == "COMPANY_A").ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EF Core Load Warning: {ex.Message}");
                LoadCleanCategories();
            }
        }

        private void LoadCleanCategories()
        {
            Tenants = new List<CompanyTenant>
            {
                new CompanyTenant { Id = 1, TenantCode = "COMPANY_A", CompanyName = "Company A (Main Store)" },
                new CompanyTenant { Id = 2, TenantCode = "COMPANY_B", CompanyName = "Company B (Branch Store)" },
                new CompanyTenant { Id = 3, TenantCode = "COMPANY_C", CompanyName = "Company C (Enterprise Store)" }
            };

            Categories = new List<Category>
            {
                new Category { Id = 1, TenantCode = ActiveTenantCode, Name = "Laptops & Notebooks", Icon = "💻", Description = "High performance gaming & workstation laptops" },
                new Category { Id = 2, TenantCode = ActiveTenantCode, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical keyboards & wireless gaming mice" },
                new Category { Id = 3, TenantCode = ActiveTenantCode, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED & High Refresh Gaming Monitors" },
                new Category { Id = 4, TenantCode = ActiveTenantCode, Name = "Storage & Memory", Icon = "💾", Description = "NVMe M.2 SSDs & High speed DDR5 RAM" },
                new Category { Id = 5, TenantCode = ActiveTenantCode, Name = "Audio & Headsets", Icon = "🎧", Description = "Studio monitors & Wireless noise canceling headsets" }
            };

            Products = new List<Product>();
            Orders = new List<Order>();
        }

        public bool AddProduct(Product product)
        {
            product.TenantCode = ActiveTenantCode;

            if (IsUsingSsmsDatabase)
            {
                try
                {
                    using var db = new MorphicDbContext();
                    db.Products.Add(product);
                    db.SaveChanges();
                    Products.Add(product);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EF Core Add Product Error: {ex.Message}");
                }
            }

            int nextId = Products.Count > 0 ? Products.Max(p => p.Id) + 1 : 1;
            product.Id = nextId;
            Products.Add(product);
            return true;
        }

        public bool UpdateProduct(Product product)
        {
            var existing = Products.FirstOrDefault(p => p.Id == product.Id);
            if (existing == null) return false;

            existing.Name = product.Name;
            existing.CategoryName = product.CategoryName;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
            existing.Description = product.Description;

            if (IsUsingSsmsDatabase)
            {
                try
                {
                    using var db = new MorphicDbContext();
                    var dbEntity = db.Products.FirstOrDefault(p => p.Id == product.Id);
                    if (dbEntity != null)
                    {
                        dbEntity.Name = product.Name;
                        dbEntity.CategoryName = product.CategoryName;
                        dbEntity.Price = product.Price;
                        dbEntity.StockQuantity = product.StockQuantity;
                        dbEntity.Description = product.Description;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EF Core Update Product Error: {ex.Message}");
                }
            }

            return true;
        }

        public bool DeleteProduct(int productId)
        {
            var prod = Products.FirstOrDefault(p => p.Id == productId);
            if (prod == null) return false;

            Products.Remove(prod);

            if (IsUsingSsmsDatabase)
            {
                try
                {
                    using var db = new MorphicDbContext();
                    var dbEntity = db.Products.FirstOrDefault(p => p.Id == productId);
                    if (dbEntity != null)
                    {
                        db.Products.Remove(dbEntity);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EF Core Delete Product Error: {ex.Message}");
                }
            }

            return true;
        }

        public decimal GetTotalRevenue() => Orders.Sum(o => o.TotalAmount);
        public int GetTotalOrders() => Orders.Count;
        public int GetTotalProductsCount() => Products.Count;
        public int GetLowStockCount() => Products.Count(p => p.IsLowStock);
    }
}
