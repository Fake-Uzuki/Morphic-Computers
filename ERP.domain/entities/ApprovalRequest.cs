using System;

namespace ERP.domain.entities
{
    public class ApprovalRequest
    {
        public const string TypeProcurementRequest = "ProcurementRequest";
        public const string TypeVoidTransaction = "VoidTransaction";
        public const string TypeCustomDiscount = "CustomDiscount";
        public const string TypeInventoryWriteOff = "InventoryWriteOff";
        public const string TypeWarrantyOverride = "WarrantyOverride";

        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusRejected = "Rejected";

        public int RequestId { get; set; }
        public int CompanyId { get; set; } = 2;
        public string RequestNumber { get; set; } = string.Empty;

        // Types: ProcurementRequest, VoidTransaction, CustomDiscount, InventoryWriteOff, WarrantyOverride
        public string RequestType { get; set; } = TypeVoidTransaction;
        public string Title { get; set; } = string.Empty;
        public string ReasonDescription { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = "Staff";
        public decimal RequestedAmount { get; set; }

        // Status: Pending, Approved, Rejected
        public string Status { get; set; } = StatusPending;
        public string? ReviewedBy { get; set; }
        public string? ReviewNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string? TargetReferenceId { get; set; }
    }
}
