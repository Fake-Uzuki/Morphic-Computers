using System;

namespace ERP.domain.entities
{
    public class ApprovalRequest
    {
        public int RequestId { get; set; }
        public int CompanyId { get; set; } = 2;
        public string RequestNumber { get; set; } = string.Empty;

        // Types: VoidTransaction, CustomDiscount, InventoryWriteOff, WarrantyOverride
        public string RequestType { get; set; } = "VoidTransaction";
        public string Title { get; set; } = string.Empty;
        public string ReasonDescription { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = "Staff";
        public decimal RequestedAmount { get; set; }

        // Status: Pending, Approved, Rejected
        public string Status { get; set; } = "Pending";
        public string? ReviewedBy { get; set; }
        public string? ReviewNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
