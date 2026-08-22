using System;
using System.Collections.Generic;
using System.Linq;
using ERP.domain.entities;
using ERP.infrastructure.data;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Data Access Service connecting ERP.winforms UI to ERP.infrastructure (EF Core MasterErpContext)
    /// and ERP.domain entities with Multi-Company support.
    /// </summary>
    public class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        public List<Company> Companies { get; private set; } = new();
        public List<Product> Products { get; private set; } = new();
        public List<Category> Categories { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();

        private readonly List<Product> _inMemoryProducts = new();
        private readonly List<Category> _inMemoryCategories = new();

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
                    db.Companies.AddRange(
                        new Company { Id = 1, Code = "MORPHIC", Name = "Morphic Computers (Main Store)", Description = "Primary Computer Store" },
                        new Company { Id = 2, Code = "APEX", Name = "Apex Cybernetics (Company B)", Description = "Branch Store Demo" },
                        new Company { Id = 3, Code = "VANGUARD", Name = "Vanguard Tech (Company C)", Description = "Enterprise Store Demo" }
                    );
                    db.SaveChanges();
                }

                if (!db.Categories.Any())
                {
                    db.Categories.AddRange(
                        // Company 1 Categories
                        new Category { CompanyId = 1, Name = "Laptops & Notebooks", Icon = "💻", Description = "Gaming & Workstation Laptops" },
                        new Category { CompanyId = 1, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical Keyboards & Wireless Mice" },
                        new Category { CompanyId = 1, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED Monitors" },
                        new Category { CompanyId = 1, Name = "Storage & Memory", Icon = "💾", Description = "NVMe SSDs & DDR5 RAM" },
                        new Category { CompanyId = 1, Name = "Audio & Headsets", Icon = "🎧", Description = "Headsets & Speakers" },

                        // Company 2 Categories
                        new Category { CompanyId = 2, Name = "Gaming Laptops", Icon = "💻", Description = "High FPS Gaming Rigs" },
                        new Category { CompanyId = 2, Name = "Gaming Accessories", Icon = "⌨️", Description = "RGB Gaming Gear" },

                        // Company 3 Categories
                        new Category { CompanyId = 3, Name = "Enterprise Servers", Icon = "🖥️", Description = "Rack Servers & Workstations" },
                        new Category { CompanyId = 3, Name = "Networking Gear", Icon = "💾", Description = "Switches & Routers" }
                    );
                    db.SaveChanges();
                }

                if (!db.Products.Any())
                {
                    db.Products.AddRange(
                        // Company 2 Seed Products
                        new Product { CompanyId = 2, Name = "Apex CyberBook 15 Pro", CategoryName = "Gaming Laptops", Price = 1599.99m, StockQuantity = 8, Description = "RTX 4070 Gaming Laptop" },
                        new Product { CompanyId = 2, Name = "Apex Cobra RGB Wireless Mouse", CategoryName = "Gaming Accessories", Price = 79.99m, StockQuantity = 15, Description = "Ultra light gaming mouse" },
                        new Product { CompanyId = 2, Name = "Apex Mechanical Keyboard RGB", CategoryName = "Gaming Accessories", Price = 129.99m, StockQuantity = 3, Description = "Custom switches" },

                        // Company 3 Seed Products
                        new Product { CompanyId = 3, Name = "Vanguard Enterprise Rack Server Z1", CategoryName = "Enterprise Servers", Price = 3499.00m, StockQuantity = 5, Description = "Dual Xeon 128GB RAM" },
                        new Product { CompanyId = 3, Name = "Vanguard 48-Port Managed Switch", CategoryName = "Networking Gear", Price = 899.99m, StockQuantity = 12, Description = "10GbE Switch" }
                    );
                    db.SaveChanges();
                }

                LoadFromDatabase();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EF Core Connection Warning: {ex.Message}");
                IsUsingSsmsDatabase = false;
                SeedInMemoryDemoStore();
            }
        }

        public void LoadFromDatabase()
        {
            if (IsUsingSsmsDatabase)
            {
                try
                {
                    using var db = new MasterErpContext();
                    Companies = db.Companies.Where(c => c.IsActive).ToList();
                    Categories = db.Categories.Where(c => c.CompanyId == ActiveCompanyId).ToList();
                    Products = db.Products.Where(p => p.CompanyId == ActiveCompanyId).ToList();
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EF Core Load Warning: {ex.Message}");
                }
            }

            // Fallback in-memory query
            Categories = _inMemoryCategories.Where(c => c.CompanyId == ActiveCompanyId).ToList();
            Products = _inMemoryProducts.Where(p => p.CompanyId == ActiveCompanyId).ToList();
        }

        private void SeedInMemoryDemoStore()
        {
            Companies = new List<Company>
            {
                new Company { Id = 1, Code = "MORPHIC", Name = "Morphic Computers (Main Store)", Description = "Primary Computer Store" },
                new Company { Id = 2, Code = "APEX", Name = "Apex Cybernetics (Company B)", Description = "Branch Store Demo" },
                new Company { Id = 3, Code = "VANGUARD", Name = "Vanguard Tech (Company C)", Description = "Enterprise Store Demo" }
            };

            _inMemoryCategories.Clear();
            _inMemoryCategories.AddRange(new[]
            {
                new Category { Id = 1, CompanyId = 1, Name = "Laptops & Notebooks", Icon = "💻", Description = "Gaming & Workstation Laptops" },
                new Category { Id = 2, CompanyId = 1, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical Keyboards & Wireless Mice" },
                new Category { Id = 3, CompanyId = 1, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED Monitors" },
                new Category { Id = 4, CompanyId = 1, Name = "Storage & Memory", Icon = "💾", Description = "NVMe SSDs & DDR5 RAM" },
                new Category { Id = 5, CompanyId = 1, Name = "Audio & Headsets", Icon = "🎧", Description = "Headsets & Speakers" },

                new Category { Id = 6, CompanyId = 2, Name = "Gaming Laptops", Icon = "💻", Description = "High FPS Gaming Rigs" },
                new Category { Id = 7, CompanyId = 2, Name = "Gaming Accessories", Icon = "⌨️", Description = "RGB Gaming Gear" },

                new Category { Id = 8, CompanyId = 3, Name = "Enterprise Servers", Icon = "🖥️", Description = "Rack Servers & Workstations" },
                new Category { Id = 9, CompanyId = 3, Name = "Networking Gear", Icon = "💾", Description = "Switches & Routers" }
            });

            _inMemoryProducts.Clear();
            _inMemoryProducts.AddRange(new[]
            {
                new Product { Id = 101, CompanyId = 2, Name = "Apex CyberBook 15 Pro", CategoryName = "Gaming Laptops", Price = 1599.99m, StockQuantity = 8, Description = "RTX 4070 Gaming Laptop" },
                new Product { Id = 102, CompanyId = 2, Name = "Apex Cobra RGB Wireless Mouse", CategoryName = "Gaming Accessories", Price = 79.99m, StockQuantity = 15, Description = "Ultra light gaming mouse" },
                new Product { Id = 103, CompanyId = 2, Name = "Apex Mechanical Keyboard RGB", CategoryName = "Gaming Accessories", Price = 129.99m, StockQuantity = 3, Description = "Custom switches" },

                new Product { Id = 201, CompanyId = 3, Name = "Vanguard Enterprise Rack Server Z1", CategoryName = "Enterprise Servers", Price = 3499.00m, StockQuantity = 5, Description = "Dual Xeon 128GB RAM" },
                new Product { Id = 202, CompanyId = 3, Name = "Vanguard 48-Port Managed Switch", CategoryName = "Networking Gear", Price = 899.99m, StockQuantity = 12, Description = "10GbE Switch" }
            });

            Categories = _inMemoryCategories.Where(c => c.CompanyId == ActiveCompanyId).ToList();
            Products = _inMemoryProducts.Where(p => p.CompanyId == ActiveCompanyId).ToList();
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

            int nextId = _inMemoryProducts.Count > 0 ? _inMemoryProducts.Max(p => p.Id) + 1 : 1;
            product.Id = nextId;
            _inMemoryProducts.Add(product);
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
            _inMemoryProducts.RemoveAll(p => p.Id == productId);

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
