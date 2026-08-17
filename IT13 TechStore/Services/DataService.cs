using System;
using System.Collections.Generic;
using System.Linq;
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

        private DataService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
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

            // Seed sample orders for realistic dashboard activity
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

        public decimal GetTotalRevenue() => Orders.Sum(o => o.TotalAmount);
        public int GetTotalOrders() => Orders.Count;
        public int GetTotalProductsCount() => Products.Count;
        public int GetLowStockCount() => Products.Count(p => p.IsLowStock);
    }
}
