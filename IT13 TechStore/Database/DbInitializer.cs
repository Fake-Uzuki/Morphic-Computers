using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using IT8_TechStore.Models;

namespace IT8_TechStore.Database
{
    /// <summary>
    /// Database Initializer utilizing Entity Framework Core (EF Core) DbContext
    /// to ensure SSMS SQL Server Master DB and Tenant Tables exist.
    /// </summary>
    public static class DbInitializer
    {
        public static bool InitializeDatabase()
        {
            try
            {
                using var db = new MorphicDbContext();
                
                // Ensure Database and Tables exist via EF Core
                db.Database.EnsureCreated();

                // Seed Default Company Tenants (Company A, Company B, Company C as drawn on whiteboard)
                if (!db.Tenants.Any())
                {
                    db.Tenants.AddRange(
                        new CompanyTenant { TenantCode = "COMPANY_A", CompanyName = "Company A (Main Store)", Description = "1st Lab Target Tenant" },
                        new CompanyTenant { TenantCode = "COMPANY_B", CompanyName = "Company B (Branch Store)", Description = "2nd Lab Target Tenant" },
                        new CompanyTenant { TenantCode = "COMPANY_C", CompanyName = "Company C (Enterprise Store)", Description = "Final Defense Target Tenant" }
                    );
                    db.SaveChanges();
                }

                // Seed Default Categories if empty
                if (!db.Categories.Any())
                {
                    db.Categories.AddRange(
                        new Category { TenantCode = "COMPANY_A", Name = "Laptops & Notebooks", Icon = "💻", Description = "Gaming & Workstation Laptops" },
                        new Category { TenantCode = "COMPANY_A", Name = "Keyboards & Mice", Icon = "⌨️", Description = "Mechanical Keyboards & Wireless Gaming Mice" },
                        new Category { TenantCode = "COMPANY_A", Name = "Monitors & Displays", Icon = "🖥️", Description = "4K OLED & High Refresh Monitors" },
                        new Category { TenantCode = "COMPANY_A", Name = "Storage & Memory", Icon = "💾", Description = "NVMe SSDs & DDR5 RAM" },
                        new Category { TenantCode = "COMPANY_A", Name = "Audio & Headsets", Icon = "🎧", Description = "Studio Monitors & ANC Headsets" }
                    );
                    db.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EF Core Init Warning: {ex.Message}");
                return false;
            }
        }
    }
}
