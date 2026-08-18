using System;
using Microsoft.Data.SqlClient;

namespace IT8_TechStore.Database
{
    public static class DbInitializer
    {
        public static bool InitializeDatabase()
        {
            string? connStr = DbConfig.GetConnectionString();
            if (string.IsNullOrEmpty(connStr))
                return false;

            try
            {
                // 1. Ensure Database exists in SSMS
                string masterConnStr = connStr.Replace($"Database={DbConfig.DatabaseName};", "Database=master;");
                using (var masterConn = new SqlConnection(masterConnStr))
                {
                    masterConn.Open();
                    string createDbSql = $@"
                        IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{DbConfig.DatabaseName}')
                        BEGIN
                            CREATE DATABASE [{DbConfig.DatabaseName}];
                        END;";
                    using var cmd = new SqlCommand(createDbSql, masterConn);
                    cmd.ExecuteNonQuery();
                }

                // 2. Ensure Tables exist
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string createTablesSql = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                        BEGIN
                            CREATE TABLE Categories (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) NOT NULL UNIQUE,
                                Icon NVARCHAR(10) NOT NULL,
                                Description NVARCHAR(255) NULL
                            );
                        END;

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                        BEGIN
                            CREATE TABLE Products (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                SKU NVARCHAR(50) NOT NULL UNIQUE,
                                Name NVARCHAR(150) NOT NULL,
                                CategoryName NVARCHAR(100) NOT NULL,
                                Price DECIMAL(18,2) NOT NULL,
                                StockQuantity INT NOT NULL,
                                Description NVARCHAR(500) NULL
                            );
                        END;";

                    using var cmd = new SqlCommand(createTablesSql, conn);
                    cmd.ExecuteNonQuery();

                    // Seed initial categories if empty
                    string checkCategories = "SELECT COUNT(*) FROM Categories";
                    using var checkCmd = new SqlCommand(checkCategories, conn);
                    int catCount = (int)checkCmd.ExecuteScalar();

                    if (catCount == 0)
                    {
                        string seedCatSql = @"
                            INSERT INTO Categories (Name, Icon, Description) VALUES
                            ('Laptops & Notebooks', '💻', 'High performance laptops & workstations'),
                            ('Keyboards & Mice', '⌨️', 'Mechanical keyboards & precision gaming mice'),
                            ('Monitors & Displays', '🖥️', '4K OLED & High Refresh Gaming Monitors'),
                            ('Storage & Memory', '💾', 'High speed NVMe SSDs & DDR5 RAM'),
                            ('Audio & Headsets', '🎧', 'Studio monitors & ANC headsets');";
                        using var seedCmd = new SqlCommand(seedCatSql, conn);
                        seedCmd.ExecuteNonQuery();
                    }

                    // Seed initial products if empty
                    string checkProducts = "SELECT COUNT(*) FROM Products";
                    using var checkProdCmd = new SqlCommand(checkProducts, conn);
                    int prodCount = (int)checkProdCmd.ExecuteScalar();

                    if (prodCount == 0)
                    {
                        string seedProdSql = @"
                            INSERT INTO Products (SKU, Name, CategoryName, Price, StockQuantity, Description) VALUES
                            ('LAP-001', 'ProBook Ultra 16 X1', 'Laptops & Notebooks', 1299.99, 12, 'Intel i9, 32GB RAM, 1TB SSD'),
                            ('LAP-002', 'Aero Blade Gaming G7', 'Laptops & Notebooks', 1899.50, 4, 'RTX 4080, Ryzen 9, 240Hz Display'),
                            ('KEY-001', 'Apex Pro Mechanical Switch', 'Keyboards & Mice', 149.99, 25, 'Hot-swappable RGB Mechanical Keyboard'),
                            ('MOU-001', 'Viper Precision Wireless Mouse', 'Keyboards & Mice', 79.99, 30, '26K DPI optical sensor, ultra light'),
                            ('MON-001', 'UltraVision 32"" 4K OLED Monitor', 'Monitors & Displays', 799.00, 3, '32-inch 4K 144Hz 0.03ms Response OLED'),
                            ('MON-002', 'Curved Gaming 27"" 180Hz', 'Monitors & Displays', 249.99, 18, '1440p QHD Curved display panel'),
                            ('SSD-001', 'Velocity Max 2TB NVMe Gen4 SSD', 'Storage & Memory', 159.99, 40, '7400MB/s Read speed with heatsink'),
                            ('RAM-001', 'Titanium RGB DDR5 32GB Kit', 'Storage & Memory', 119.50, 2, '6000MHz CL30 Dual Channel Memory'),
                            ('AUD-001', 'SonicStudio Pro Noise Canceling Headset', 'Audio & Headsets', 199.99, 15, 'Active ANC, spatial audio, 40hr battery');";
                        using var seedProdCmd = new SqlCommand(seedProdSql, conn);
                        seedProdCmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SSMS Init Warning: {ex.Message}");
                return false;
            }
        }
    }
}
