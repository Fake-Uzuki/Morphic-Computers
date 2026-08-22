using System;
using System.Collections.Generic;
using System.Linq;
using ERP.domain.entities;
using ERP.infrastructure.data;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Data Access Service connecting ERP.winforms UI to ERP.infrastructure (EF Core MasterErpContext)
    /// and ERP.domain entities for Morphic Computers.
    /// </summary>
    public class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        public List<Company> Companies { get; private set; } = new();
        public List<Product> Products { get; private set; } = new();
        public List<Category> Categories { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();

        public int ActiveCompanyId { get; set; } = 1;
        public bool IsUsingSsmsDatabase { get; private set; }

        private DataService()
        {
            InitializeDataStore();
        }

        private void InitializeDataStore()
        {
            try
            {
                using var db = new MasterErpContext();
                IsUsingSsmsDatabase = db.Database.EnsureCreated();

                if (!db.Companies.Any())
                {
                    db.Companies.Add(new Company { Id = 1, Code = "MORPHIC", Name = "Morphic Computers", Description = "Main Computer Store" });
                    db.SaveChanges();
                }

                if (!db.Categories.Any())
                {
                    db.Categories.AddRange(
                        new Category { CompanyId = 1, Name = "Laptops & Notebooks", Icon = "💻", Description = "Gaming & Workstation Laptops" },
                        new Category { CompanyId = 1, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical Keyboards & Wireless Gaming Mice" },
                        new Category { CompanyId = 1, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED & High Refresh Monitors" },
                        new Category { CompanyId = 1, Name = "Storage & Memory", Icon = "💾", Description = "NVMe SSDs & DDR5 RAM" },
                        new Category { CompanyId = 1, Name = "Audio & Headsets", Icon = "🎧", Description = "Studio Monitors & ANC Headsets" }
                    );
                    db.SaveChanges();
                }

                LoadFromDatabase();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EF Core Connection Warning: {ex.Message}");
                IsUsingSsmsDatabase = false;
                LoadCleanCategories();
            }
        }

        public void LoadFromDatabase()
        {
            try
            {
                using var db = new MasterErpContext();
                Companies = db.Companies.Where(c => c.IsActive).ToList();
                Categories = db.Categories.Where(c => c.CompanyId == ActiveCompanyId).ToList();
                Products = db.Products.Where(p => p.CompanyId == ActiveCompanyId).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EF Core Load Warning: {ex.Message}");
                LoadCleanCategories();
            }
        }

        private void LoadCleanCategories()
        {
            Companies = new List<Company>
            {
                new Company { Id = 1, Code = "MORPHIC", Name = "Morphic Computers", Description = "Main Computer Store" }
            };

            Categories = new List<Category>
            {
                new Category { Id = 1, CompanyId = 1, Name = "Laptops & Notebooks", Icon = "💻", Description = "High performance gaming & workstation laptops" },
                new Category { Id = 2, CompanyId = 1, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical keyboards & wireless gaming mice" },
                new Category { Id = 3, CompanyId = 1, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED & High Refresh Gaming Monitors" },
                new Category { Id = 4, CompanyId = 1, Name = "Storage & Memory", Icon = "💾", Description = "NVMe M.2 SSDs & High speed DDR5 RAM" },
                new Category { Id = 5, CompanyId = 1, Name = "Audio & Headsets", Icon = "🎧", Description = "Studio monitors & Wireless noise canceling headsets" }
            };

            Products = new List<Product>();
            Orders = new List<Order>();
        }

        public bool AddProduct(Product product)
        {
            product.CompanyId = ActiveCompanyId;

            if (IsUsingSsmsDatabase)
            {
                try
                {
                    using var db = new MasterErpContext();
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
                    using var db = new MasterErpContext();
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
                    using var db = new MasterErpContext();
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
