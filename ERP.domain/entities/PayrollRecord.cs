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
        public decimal GrossPay => BaseSalary + OvertimePay + CommissionAmount;

        // Philippine Statutory Deductions
        public decimal SssDeduction { get; set; }
        public decimal PhilHealthDeduction { get; set; }
        public decimal PagIbigDeduction { get; set; }
        public decimal WithholdingTax { get; set; }
        public decimal OtherDeductions { get; set; }

        // Total Deductions (sum of statutory deductions + other deductions)
        public decimal Deductions { get; set; }
        public decimal NetPay => GrossPay - Deductions;

        public string Status { get; set; } = "Paid"; // Draft, Approved, Paid
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; } = "Manager";
    }
}
