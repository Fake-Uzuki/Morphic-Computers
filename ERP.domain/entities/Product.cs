using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.domain.entities
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }

        // Backward compatibility helpers for WinForms UI
        [NotMapped]
        public int Id { get => ProductId; set => ProductId = value; }
        [NotMapped]
        public string SKU { get => ProductCode; set => ProductCode = value; }
        [NotMapped]
        public string Name { get => ProductName; set => ProductName = value; }
        [NotMapped]
        public decimal Price { get => UnitPrice; set => UnitPrice = value; }
        [NotMapped]
        public int CompanyId { get; set; } = 1;

        public string CategoryName { get; set; } = "Graphics Cards (GPU)";
        public string? SupplierName { get; set; } = "Direct Distribution";
        public int? SupplierId { get; set; }

        [NotMapped]
        public int StockQuantity { get; set; }

        public string? Description { get; set; } = string.Empty;

        public bool IsLowStock => StockQuantity <= 5;
        public string StockStatus => StockQuantity switch
        {
            0 => "Out of Stock",
            <= 5 => $"Low Stock ({StockQuantity})",
            _ => $"In Stock ({StockQuantity})"
        };
    }
}
