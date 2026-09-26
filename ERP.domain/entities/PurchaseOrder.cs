using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ERP.domain.entities
{
    public class PurchaseOrder
    {
        public int PurchaseOrderId { get; set; }
        public int CompanyId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; } = "Admin";
        public string? ApprovedBy { get; set; } = "Procurement Lead";
        public DateTime? ReceivedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<PurchaseOrderItem> Items { get; set; } = new();

        // UI & backward compatibility helpers for WinForms DataGridView
        [NotMapped]
        public string PoNumber
        {
            get => PurchaseOrderNumber;
            set => PurchaseOrderNumber = value;
        }

        [NotMapped]
        public string ItemDescription
        {
            get
            {
                if (Items != null && Items.Count > 0)
                {
                    if (Items.Count == 1) return Items[0].ItemDescription;
                    return $"{Items[0].ItemDescription} (+{Items.Count - 1} more)";
                }
                return Notes ?? string.Empty;
            }
        }

        [NotMapped]
        public int Quantity => Items?.Sum(i => i.Quantity) ?? 0;

        [NotMapped]
        public decimal UnitCost
        {
            get
            {
                if (Items != null && Items.Count == 1) return Items[0].UnitCost;
                var totalQty = Quantity;
                return totalQty > 0 ? Math.Round(TotalAmount / totalQty, 2) : 0;
            }
        }
    }
}
