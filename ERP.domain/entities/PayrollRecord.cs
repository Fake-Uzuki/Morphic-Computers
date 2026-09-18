using System;

namespace ERP.domain.entities
{
    public class PayrollRecord
    {
        public int PayrollId { get; set; }
        public int CompanyId { get; set; } = 2;
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPay => (BaseSalary + OvertimePay + CommissionAmount) - Deductions;

        public string Status { get; set; } = "Paid"; // Draft, Approved, Paid
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; } = "Manager";
    }
}
