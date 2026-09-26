using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.domain.security;
using ERP.infrastructure.data;
using ERP.infrastructure.services;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    [Route("api/tenant/{companyId:int}/financial-statements")]
    [Route("api/tenant/{companyId:int}/finance")]
    public class FinancialStatementsController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;

        public FinancialStatementsController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            if (companyId <= 0)
            {
                return (false, "Super Admin or platform-level callers cannot perform tenant operational financial statements.", 403);
            }

            var company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                return (false, $"Company ID {companyId} not found.", 404);
            }

            if (!ModuleAccessService.IsModuleEnabled(company.PlanName, "FinancialStatements"))
            {
                return (false, $"Plan '{company.PlanName}' does not include access to the Financial Statements module. Upgrade to Medium to enable Financial Statements.", 403);
            }

            return (true, null, 200);
        }

        [HttpGet]
        public async Task<IActionResult> GetFinancialStatement(
            int companyId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? period = null)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            // Period shortcut resolution
            string periodLabel = "All Time";
            if (string.Equals(period, "current_month", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(period, "this_month", StringComparison.OrdinalIgnoreCase))
            {
                startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
                periodLabel = $"{startDate.Value:MMMM yyyy} (Current Month)";
            }
            else if (string.Equals(period, "previous_month", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(period, "last_month", StringComparison.OrdinalIgnoreCase))
            {
                var firstThisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                startDate = firstThisMonth.AddMonths(-1);
                endDate = firstThisMonth.AddTicks(-1);
                periodLabel = $"{startDate.Value:MMMM yyyy} (Previous Month)";
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                periodLabel = $"{startDate.Value:MMM dd, yyyy} — {endDate.Value:MMM dd, yyyy}";
            }
            else if (startDate.HasValue)
            {
                periodLabel = $"From {startDate.Value:MMM dd, yyyy}";
            }
            else if (endDate.HasValue)
            {
                periodLabel = $"Until {endDate.Value:MMM dd, yyyy}";
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);

                // 1. Retail Sales Revenue (exclude Voided and Cancelled orders)
                var ordersQuery = tenantDb.Orders.AsNoTracking()
                    .Where(o => o.CompanyId == companyId);

                if (startDate.HasValue) ordersQuery = ordersQuery.Where(o => o.CreatedAt >= startDate.Value);
                if (endDate.HasValue) ordersQuery = ordersQuery.Where(o => o.CreatedAt <= endDate.Value);

                var ordersList = await ordersQuery.ToListAsync();
                var validOrders = ordersList.Where(o => o.Status != "Voided" && o.Status != "Cancelled").ToList();
                decimal retailSales = validOrders.Sum(o => o.TotalAmount);
                decimal vatCollected = validOrders.Sum(o => o.Tax);

                // 2. Repair Services Revenue (Completed/Released only)
                var repairsQuery = tenantDb.RepairTickets.AsNoTracking()
                    .Where(t => t.CompanyId == companyId && t.IsActive &&
                                (t.Status == "Completed" || t.Status == "Released"));

                if (startDate.HasValue) repairsQuery = repairsQuery.Where(t => (t.CompletedAt ?? t.CreatedAt) >= startDate.Value);
                if (endDate.HasValue) repairsQuery = repairsQuery.Where(t => (t.CompletedAt ?? t.CreatedAt) <= endDate.Value);

                var repairsList = await repairsQuery.ToListAsync();
                decimal repairSales = repairsList.Sum(t => t.TotalAmount);

                // 3. Operating Overhead Expenses (Active only)
                var expensesQuery = tenantDb.Expenses.AsNoTracking()
                    .Where(e => e.CompanyId == companyId && e.IsActive);

                if (startDate.HasValue) expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= startDate.Value);
                if (endDate.HasValue) expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= endDate.Value);

                var expensesList = await expensesQuery.ToListAsync();
                decimal operatingOverhead = expensesList.Sum(e => e.Amount);

                // 4. Labor & Payroll Compensation (Gross Pay represents total employer wage liability)
                var payrollQuery = tenantDb.PayrollRecords.AsNoTracking()
                    .Where(p => p.CompanyId == companyId && p.Status != "Voided");

                if (startDate.HasValue) payrollQuery = payrollQuery.Where(p => p.PeriodEnd >= startDate.Value);
                if (endDate.HasValue) payrollQuery = payrollQuery.Where(p => p.PeriodStart <= endDate.Value);

                var payrollList = await payrollQuery.ToListAsync();
                decimal payrollCosts = payrollList.Sum(p => p.GrossPay);

                var report = new FinancialStatementReport
                {
                    CompanyId = companyId,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    PeriodLabel = periodLabel,
                    RetailSalesRevenue = retailSales,
                    RepairServicesRevenue = repairSales,
                    CostOfGoodsSold = 0.00m,
                    CogsStatus = "Unrecorded (Historical unit purchase cost is not tracked in product inventory catalog)",
                    PayrollExpenses = payrollCosts,
                    OperatingOverhead = operatingOverhead,
                    VatLiability = vatCollected,
                    CompletedOrdersCount = validOrders.Count,
                    CompletedRepairsCount = repairsList.Count,
                    ActiveExpensesCount = expensesList.Count,
                    PayrollDisbursementsCount = payrollList.Count,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetFinancialStatement error: {ex.Message}");
                return StatusCode(500, new { error = $"Failed to calculate financial statement: {ex.Message}" });
            }
        }
    }
}
