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
                LoadCleanCategories();
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
                            SKU = reader.IsDBNull(1) ? "" : reader.GetString(1),
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
                LoadCleanCategories();
            }
        }

        private void LoadCleanCategories()
        {
            Categories = new List<Category>
            {
                new Category { Id = 1, Name = "Laptops & Notebooks", Icon = "💻", Description = "High performance gaming & workstation laptops" },
                new Category { Id = 2, Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical keyboards & wireless gaming mice" },
                new Category { Id = 3, Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED & High Refresh Gaming Monitors" },
                new Category { Id = 4, Name = "Storage & Memory", Icon = "💾", Description = "NVMe M.2 SSDs & High speed DDR5 RAM" },
                new Category { Id = 5, Name = "Audio & Headsets", Icon = "🎧", Description = "Studio monitors & Wireless noise canceling headsets" }
            };

            Products = new List<Product>();
            Orders = new List<Order>();
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
                        cmd.Parameters.AddWithValue("@sku", product.SKU ?? "");
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
                        cmd.Parameters.AddWithValue("@sku", product.SKU ?? "");
                        cmd.Parameters.AddWithValue("@name", product.Name);
                        cmd.Parameters.AddWithValue("@cat", product.CategoryName);
                        cmd.Parameters.AddWithValue("@price", product.Price);
                        cmd.Parameters.AddWithValue("@qty", product.StockQuantity);
                        cmd.Parameters.AddWithValue("@desc", (object?)product.Description ?? DBNull.Value);

                        cmd.ExecuteNonQuery();

                        var existing = Products.FirstOrDefault(p => p.Id == product.Id);
                        if (existing != null)
                        {
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
                item.Name = product.Name;
                item.CategoryName = product.CategoryName;
                item.Price = product.Price;
                item.StockQuantity = product.StockQuantity;
                item.Description = product.Description ?? string.Empty;
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
