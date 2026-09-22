using System;

namespace ERP.domain.entities
{
    public class ExpenseRecord
    {
        public int ExpenseId { get; set; }
        public int CompanyId { get; set; } = 2;
        public string ExpenseNumber { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = "Store Utilities";
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaidTo { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Cash";
        public string RecordedBy { get; set; } = "Admin";
        public string? ReceiptRef { get; set; }
        public bool IsTaxDeductible { get; set; } = true;
    }
}
