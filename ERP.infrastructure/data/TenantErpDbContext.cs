using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;

namespace ERP.infrastructure.data
{
    public class TenantErpDbContext : DbContext
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Category> Categories => Set<Category>();

        public TenantErpDbContext(DbContextOptions<TenantErpDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>(entity =>
            {
                entity.HasKey(x => x.ProductId);

                entity.Property(x => x.ProductCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CategoryName)
                    .HasMaxLength(100)
                    .HasDefaultValue("Graphics Cards (GPU)");

                entity.Property(x => x.Description)
                    .HasMaxLength(1000)
                    .IsRequired(false);

                entity.HasIndex(x => x.ProductCode)
                    .IsUnique();
            });

            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.CustomerCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CustomerName)
                    .HasMaxLength(200)
                    .IsRequired();
            });

            builder.Entity<Supplier>(entity =>
            {
                entity.HasKey(x => x.SupplierId);

                entity.Property(x => x.SupplierCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.SupplierName)
                    .HasMaxLength(200)
                    .IsRequired();
            });

            builder.Entity<Inventory>(entity =>
            {
                entity.HasKey(x => x.InventoryId);

                entity.Property(x => x.QuantityOnHand)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ReorderLevel)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Order>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(100);
                entity.Property(x => x.CustomerName).HasMaxLength(200);
                entity.Property(x => x.PaymentMethod).HasMaxLength(50);
                entity.Property(x => x.Subtotal).HasPrecision(18, 2);
                entity.Property(x => x.Tax).HasPrecision(18, 2);
                entity.Property(x => x.Discount).HasPrecision(18, 2);
                entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
                entity.Ignore(x => x.Items);
                entity.Ignore(x => x.Status);
                entity.Ignore(x => x.ArchivedAt);
            });

            builder.Entity<Category>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Icon).HasMaxLength(50).IsRequired(false);
                entity.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            });
        }
    }
}
