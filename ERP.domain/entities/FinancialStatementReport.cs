using System;

namespace ERP.domain.entities
{
    /// <summary>
    /// Financial Profit & Loss Statement model aggregating real tenant transactions.
    /// Sourced directly from SQL-backed Orders, RepairTickets, Expenses, and PayrollRecords.
    /// </summary>
    public class FinancialStatementReport
    {
        public int CompanyId { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public string PeriodLabel { get; set; } = "All Time";

        // 1. Revenue Inflow
        public decimal RetailSalesRevenue { get; set; }
        public decimal RepairServicesRevenue { get; set; }
        public decimal GrossRevenue => RetailSalesRevenue + RepairServicesRevenue;

        // 2. Expenditures & Cost of Sales
        public decimal CostOfGoodsSold { get; set; } = 0.00m;
        public string CogsStatus { get; set; } = "Unrecorded (Historical unit purchase cost is not tracked in product inventory catalog)";
        public decimal PayrollExpenses { get; set; } // Total employer gross compensation liability
        public decimal OperatingOverhead { get; set; } // Total active store operating expenses
        public decimal TotalExpenses => CostOfGoodsSold + PayrollExpenses + OperatingOverhead;

        // 3. Profitability & Tax
        public decimal NetOperatingProfit => GrossRevenue - TotalExpenses;
        public decimal VatLiability { get; set; } // 12% statutory VAT collected from valid retail sales

        // Transaction Counts
        public int CompletedOrdersCount { get; set; }
        public int CompletedRepairsCount { get; set; }
        public int ActiveExpensesCount { get; set; }
        public int PayrollDisbursementsCount { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
