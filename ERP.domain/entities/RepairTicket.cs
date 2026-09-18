using System;

namespace ERP.domain.entities
{
    public class RepairTicket
    {
        public int RepairTicketId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public int CompanyId { get; set; } = 2;

        // Customer details
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Device information
        public string DeviceType { get; set; } = "Desktop PC"; // Desktop PC, Laptop, GPU, Phone, Console
        public string DeviceBrandModel { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }

        // Diagnostic & Service Details
        public string ReportedIssue { get; set; } = string.Empty;
        public string? DiagnosticNotes { get; set; }
        public string? AssignedTechnician { get; set; } = "Hardware Tech";
        
        // Status pipeline: Received, Diagnosing, AwaitingParts, InRepair, ReadyForPickup, Completed, Cancelled
        public string Status { get; set; } = "Received";

        // Financial & Billing
        public decimal LaborFee { get; set; }
        public decimal PartsCost { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal TotalAmount => LaborFee + PartsCost;
        public decimal BalanceDue => Math.Max(0, TotalAmount - DepositAmount);

        // Dates & Warranty
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? WarrantyTerms { get; set; } = "30-Day Service Warranty on replaced parts and labor.";
        public bool IsActive { get; set; } = true;
    }
}
