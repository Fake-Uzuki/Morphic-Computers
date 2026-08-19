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
                                SKU NVARCHAR(50) NULL,
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
                            ('Laptops & Notebooks', '💻', 'High performance gaming & workstation laptops'),
                            ('Keyboards & Mice', '⌨️', 'Mechanical keyboards & wireless gaming mice'),
                            ('Monitors & Displays', '🖥️', '4K OLED & High Refresh Gaming Monitors'),
                            ('Storage & Memory', '💾', 'High speed NVMe SSDs & DDR5 RAM'),
                            ('Audio & Headsets', '🎧', 'Studio monitors & ANC headsets');";
                        using var seedCmd = new SqlCommand(seedCatSql, conn);
                        seedCmd.ExecuteNonQuery();
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
