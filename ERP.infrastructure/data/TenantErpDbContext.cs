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
        public DbSet<RepairTicket> RepairTickets => Set<RepairTicket>();
        public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
        public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
        public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
        public DbSet<StorePolicy> StorePolicies => Set<StorePolicy>();

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

                entity.Property(x => x.SupplierName)
                    .HasMaxLength(200)
                    .IsRequired(false);

                entity.Property(x => x.SupplierId)
                    .IsRequired(false);
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
                entity.Property(x => x.CashierName).IsRequired(false);
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

            builder.Entity<RepairTicket>(entity =>
            {
                entity.HasKey(x => x.RepairTicketId);
                entity.Property(x => x.TicketNumber).HasMaxLength(50).IsRequired();
                entity.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.CustomerPhone).HasMaxLength(50).IsRequired(false);
                entity.Property(x => x.CustomerEmail).HasMaxLength(100).IsRequired(false);
                entity.Property(x => x.DeviceType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.DeviceBrandModel).HasMaxLength(200).IsRequired();
                entity.Property(x => x.SerialNumber).HasMaxLength(100).IsRequired(false);
                entity.Property(x => x.ReportedIssue).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.DiagnosticNotes).HasMaxLength(2000).IsRequired(false);
                entity.Property(x => x.AssignedTechnician).HasMaxLength(100).IsRequired(false);
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.LaborFee).HasPrecision(18, 2);
                entity.Property(x => x.PartsCost).HasPrecision(18, 2);
                entity.Property(x => x.DepositAmount).HasPrecision(18, 2);
                entity.Property(x => x.WarrantyTerms).HasMaxLength(500).IsRequired(false);
                entity.Ignore(x => x.TotalAmount);
                entity.Ignore(x => x.BalanceDue);
                entity.Property(x => x.PartsSupplier).IsRequired(false);
            });

            builder.Entity<StaffMember>(entity =>
            {
                entity.HasKey(x => x.StaffId);
                entity.Property(x => x.StaffCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Role).HasMaxLength(100).IsRequired();
                entity.Property(x => x.PositionTitle).HasMaxLength(100).IsRequired();
                entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
                entity.Property(x => x.MonthlySalary).HasPrecision(18, 2);
                entity.Ignore(x => x.InitialPassword);
            });

            builder.Entity<ApprovalRequest>(entity =>
            {
                entity.HasKey(x => x.RequestId);
                entity.Property(x => x.RequestNumber).HasMaxLength(50).IsRequired();
                entity.Property(x => x.RequestType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ReasonDescription).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.RequestedBy).HasMaxLength(100).IsRequired();
                entity.Property(x => x.RequestedAmount).HasPrecision(18, 2);
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.ReviewedBy).HasMaxLength(100).IsRequired(false);
                entity.Property(x => x.ReviewNotes).HasMaxLength(1000).IsRequired(false);
                entity.Property(x => x.TargetReferenceId).IsRequired(false);
            });

            builder.Entity<PayrollRecord>(entity =>
            {
                entity.HasKey(x => x.PayrollId);
                entity.Property(x => x.StaffName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Role).HasMaxLength(100).IsRequired();
                entity.Property(x => x.BaseSalary).HasPrecision(18, 2);
                entity.Property(x => x.OvertimePay).HasPrecision(18, 2);
                entity.Property(x => x.CommissionAmount).HasPrecision(18, 2);
                entity.Property(x => x.Deductions).HasPrecision(18, 2);
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.PaymentMethod).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ProcessedBy).HasMaxLength(100).IsRequired();
                entity.Ignore(x => x.NetPay);
            });

            builder.Entity<StorePolicy>(entity =>
            {
                entity.HasKey(x => x.PolicyId);
                entity.Property(x => x.PolicyType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ContentText).HasMaxLength(4000).IsRequired();
                entity.Property(x => x.LastUpdatedBy).HasMaxLength(100).IsRequired();
            });
        }
    }
}
