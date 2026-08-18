using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using IT8_TechStore.Database;
using IT8_TechStore.Models;

namespace IT8_TechStore.Services
{
    public class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        public List<Product> Products { get; private set; } = new();
        public List<Category> Categories { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();

        public bool IsUsingSsmsDatabase { get; private set; }

        private DataService()
        {
            InitializeDataStore();
        }

        private void InitializeDataStore()
        {
            // Attempt SSMS SQL Server Initialization
            IsUsingSsmsDatabase = DbInitializer.InitializeDatabase();

            if (IsUsingSsmsDatabase)
            {
                LoadFromDatabase();
            }
            else
            {
                SeedInitialInMemoryData();
            }
        }

        public void LoadFromDatabase()
        {
            string? connStr = DbConfig.GetConnectionString();
            if (string.IsNullOrEmpty(connStr)) return;

            try
            {
                using var conn = new SqlConnection(connStr);
                conn.Open();

                // Load Categories from SSMS
                var newCats = new List<Category>();
                string catSql = "SELECT Id, Name, Icon, Description FROM Categories";
                using (var catCmd = new SqlCommand(catSql, conn))
                using (var reader = catCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        newCats.Add(new Category
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Icon = reader.IsDBNull(2) ? "💻" : reader.GetString(2),
                            Description = reader.IsDBNull(3) ? "" : reader.GetString(3)
                        });
                    }
                }
                Categories = newCats;

                // Load Products from SSMS
                var newProds = new List<Product>();
                string prodSql = "SELECT Id, SKU, Name, CategoryName, Price, StockQuantity, Description FROM Products";
                using (var prodCmd = new SqlCommand(prodSql, conn))
                using (var reader = prodCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        newProds.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            SKU = reader.GetString(1),
                            Name = reader.GetString(2),
                            CategoryName = reader.GetString(3),
                            Price = reader.GetDecimal(4),
                            StockQuantity = reader.GetInt32(5),
                            Description = reader.IsDBNull(6) ? "" : reader.GetString(6)
                        });
                    }
                }
                Products = newProds;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading from SSMS DB: {ex.Message}");
                SeedInitialInMemoryData();
            }
        }

        private void SeedInitialInMemoryData()
        {
            Categories = new List<Category>
            {
                new Category { Id = 1, Name = "Laptops & Notebooks", Icon = "💻", Description = "High performance gaming & workstation laptops" },
                new Category { Id = 2, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical keyboards & wireless gaming mice" },
                new Category { Id = 3, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED & High Refresh Gaming Monitors" },
                new Category { Id = 4, Name = "Storage & Memory", Icon = "💾", Description = "NVMe M.2 SSDs & High speed DDR5 RAM" },
                new Category { Id = 5, Name = "Audio & Headsets", Icon = "🎧", Description = "Studio monitors & Wireless noise canceling headsets" }
            };

            Products = new List<Product>
            {
                new Product { Id = 1, SKU = "LAP-001", Name = "ProBook Ultra 16 X1", CategoryName = "Laptops & Notebooks", Price = 1299.99m, StockQuantity = 12, Description = "Intel i9, 32GB RAM, 1TB SSD" },
                new Product { Id = 2, SKU = "LAP-002", Name = "Aero Blade Gaming G7", CategoryName = "Laptops & Notebooks", Price = 1899.50m, StockQuantity = 4, Description = "RTX 4080, Ryzen 9, 240Hz Display" },
                new Product { Id = 3, SKU = "KEY-001", Name = "Apex Pro Mechanical Switch", CategoryName = "Keyboards & Mice", Price = 149.99m, StockQuantity = 25, Description = "Hot-swappable RGB Mechanical Keyboard" },
                new Product { Id = 4, SKU = "MOU-001", Name = "Viper Precision Wireless Mouse", CategoryName = "Keyboards & Mice", Price = 79.99m, StockQuantity = 30, Description = "26K DPI optical sensor, ultra light" },
                new Product { Id = 5, SKU = "MON-001", Name = "UltraVision 32\" 4K OLED Monitor", CategoryName = "Monitors & Displays", Price = 799.00m, StockQuantity = 3, Description = "32-inch 4K 144Hz 0.03ms Response OLED" },
                new Product { Id = 6, SKU = "MON-002", Name = "Curved Gaming 27\" 180Hz", CategoryName = "Monitors & Displays", Price = 249.99m, StockQuantity = 18, Description = "1440p QHD Curved display panel" },
                new Product { Id = 7, SKU = "SSD-001", Name = "Velocity Max 2TB NVMe Gen4 SSD", CategoryName = "Storage & Memory", Price = 159.99m, StockQuantity = 40, Description = "7400MB/s Read speed with heatsink" },
                new Product { Id = 8, SKU = "RAM-001", Name = "Titanium RGB DDR5 32GB Kit", CategoryName = "Storage & Memory", Price = 119.50m, StockQuantity = 2, Description = "6000MHz CL30 Dual Channel Memory" },
                new Product { Id = 9, SKU = "AUD-001", Name = "SonicStudio Pro Noise Canceling Headset", CategoryName = "Audio & Headsets", Price = 199.99m, StockQuantity = 15, Description = "Active ANC, spatial audio, 40hr battery" }
            };

            Orders = new List<Order>
            {
                new Order
                {
                    Id = "ORD-20260817-001",
                    CustomerName = "John Doe",
                    CreatedAt = DateTime.Now.AddHours(-3),
                    Subtotal = 1449.98m,
                    Discount = 50.00m,
                    Tax = 70.00m,
                    TotalAmount = 1469.98m,
                    PaymentMethod = "Credit Card",
                    Items = new List<CartItem>
                    {
                        new CartItem { Product = Products[0], Quantity = 1 },
                        new CartItem { Product = Products[2], Quantity = 1 }
                    }
                },
                new Order
                {
                    Id = "ORD-20260817-002",
                    CustomerName = "Sarah Jenkins",
                    CreatedAt = DateTime.Now.AddHours(-1),
                    Subtotal = 239.98m,
                    Discount = 0m,
                    Tax = 12.00m,
                    TotalAmount = 251.98m,
                    PaymentMethod = "Cash",
                    Items = new List<CartItem>
                    {
                        new CartItem { Product = Products[3], Quantity = 1 },
                        new CartItem { Product = Products[6], Quantity = 1 }
                    }
                }
            };
        }

        public bool AddProduct(Product product)
        {
            if (IsUsingSsmsDatabase)
            {
                string? connStr = DbConfig.GetConnectionString();
                if (!string.IsNullOrEmpty(connStr))
                {
                    try
                    {
                        using var conn = new SqlConnection(connStr);
                        conn.Open();
                        string sql = @"
                            INSERT INTO Products (SKU, Name, CategoryName, Price, StockQuantity, Description) 
                            VALUES (@sku, @name, @cat, @price, @qty, @desc);
                            SELECT SCOPE_IDENTITY();";

                        using var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@sku", product.SKU);
                        cmd.Parameters.AddWithValue("@name", product.Name);
                        cmd.Parameters.AddWithValue("@cat", product.CategoryName);
                        cmd.Parameters.AddWithValue("@price", product.Price);
                        cmd.Parameters.AddWithValue("@qty", product.StockQuantity);
                        cmd.Parameters.AddWithValue("@desc", (object?)product.Description ?? DBNull.Value);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            product.Id = Convert.ToInt32(result);
                        }

                        Products.Add(product);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SSMS Add Error: {ex.Message}");
                    }
                }
            }

            // In-Memory Fallback
            product.Id = Products.Count > 0 ? Products.Max(p => p.Id) + 1 : 1;
            Products.Add(product);
            return true;
        }

        public bool UpdateProduct(Product product)
        {
            if (IsUsingSsmsDatabase)
            {
                string? connStr = DbConfig.GetConnectionString();
                if (!string.IsNullOrEmpty(connStr))
                {
                    try
                    {
                        using var conn = new SqlConnection(connStr);
                        conn.Open();
                        string sql = @"
                            UPDATE Products 
                            SET SKU = @sku, Name = @name, CategoryName = @cat, Price = @price, StockQuantity = @qty, Description = @desc
                            WHERE Id = @id;";

                        using var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", product.Id);
                        cmd.Parameters.AddWithValue("@sku", product.SKU);
                        cmd.Parameters.AddWithValue("@name", product.Name);
                        cmd.Parameters.AddWithValue("@cat", product.CategoryName);
                        cmd.Parameters.AddWithValue("@price", product.Price);
                        cmd.Parameters.AddWithValue("@qty", product.StockQuantity);
                        cmd.Parameters.AddWithValue("@desc", (object?)product.Description ?? DBNull.Value);

                        cmd.ExecuteNonQuery();

                        var existing = Products.FirstOrDefault(p => p.Id == product.Id);
                        if (existing != null)
                        {
                            existing.SKU = product.SKU;
                            existing.Name = product.Name;
                            existing.CategoryName = product.CategoryName;
                            existing.Price = product.Price;
                            existing.StockQuantity = product.StockQuantity;
                            existing.Description = product.Description ?? string.Empty;
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SSMS Update Error: {ex.Message}");
                    }
                }
            }

            // In-Memory Fallback
            var item = Products.FirstOrDefault(p => p.Id == product.Id);
            if (item != null)
            {
                item.SKU = product.SKU;
                item.Name = product.Name;
                item.CategoryName = product.CategoryName;
                item.Price = product.Price;
                item.StockQuantity = product.StockQuantity;
                item.Description = product.Description;
                return true;
            }
            return false;
        }

        public bool DeleteProduct(int productId)
        {
            if (IsUsingSsmsDatabase)
            {
                string? connStr = DbConfig.GetConnectionString();
                if (!string.IsNullOrEmpty(connStr))
                {
                    try
                    {
                        using var conn = new SqlConnection(connStr);
                        conn.Open();
                        string sql = "DELETE FROM Products WHERE Id = @id;";
                        using var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", productId);
                        cmd.ExecuteNonQuery();

                        Products.RemoveAll(p => p.Id == productId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SSMS Delete Error: {ex.Message}");
                    }
                }
            }

            // In-Memory Fallback
            Products.RemoveAll(p => p.Id == productId);
            return true;
        }

        public decimal GetTotalRevenue() => Orders.Sum(o => o.TotalAmount);
        public int GetTotalOrders() => Orders.Count;
        public int GetTotalProductsCount() => Products.Count;
        public int GetLowStockCount() => Products.Count(p => p.IsLowStock);
    }
}
