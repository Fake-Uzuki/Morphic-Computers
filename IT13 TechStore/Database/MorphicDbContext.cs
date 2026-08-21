using Microsoft.EntityFrameworkCore;
using IT8_TechStore.Models;

namespace IT8_TechStore.Database
{
    /// <summary>
    /// Entity Framework Core DbContext mapping C# Model blueprints
    /// directly to SSMS SQL Server tables as drawn on the project whiteboard.
    /// </summary>
    public class MorphicDbContext : DbContext
    {
        public DbSet<CompanyTenant> Tenants { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;

        public MorphicDbContext()
        {
        }

        public MorphicDbContext(DbContextOptions<MorphicDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string? connStr = DbConfig.GetConnectionString();
                if (!string.IsNullOrEmpty(connStr))
                {
                    optionsBuilder.UseSqlServer(connStr);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. CompanyTenant Entity Mapping
            modelBuilder.Entity<CompanyTenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenantCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(150);
            });

            // 2. Category Entity Mapping
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TenantCode).HasMaxLength(50).HasDefaultValue("COMPANY_A");
            });

            // 3. Product Entity Mapping
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TenantCode).HasMaxLength(50).HasDefaultValue("COMPANY_A");
            });
        }
    }
}
