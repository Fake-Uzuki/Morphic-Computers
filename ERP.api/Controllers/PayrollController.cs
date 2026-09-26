using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.domain.services;
using ERP.infrastructure.data;
using ERP.infrastructure.services;
using ERP.domain.security;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class PayrollController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public PayrollController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            if (companyId <= 0)
            {
                return (false, "Super Admin or platform-level callers cannot perform tenant operational payroll.", 403);
            }

            var company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                return (false, $"Company ID {companyId} not found.", 404);
            }

            if (!ModuleAccessService.IsModuleEnabled(company.PlanName, "Payroll"))
            {
                return (false, $"Plan '{company.PlanName}' does not include access to the Payroll module. Upgrade to Medium to enable Payroll.", 403);
            }

            return (true, null, 200);
        }

        private static async Task EnsurePayrollSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PayrollRecords')
BEGIN
    CREATE TABLE PayrollRecords (
        PayrollId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL DEFAULT 2,
        StaffId INT NOT NULL,
        StaffName NVARCHAR(200) NOT NULL,
        Role NVARCHAR(100) NOT NULL,
        PeriodStart DATETIME2 NOT NULL,
        PeriodEnd DATETIME2 NOT NULL,
        BaseSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
        OvertimePay DECIMAL(18,2) NOT NULL DEFAULT 0,
        CommissionAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        SssDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,
        PhilHealthDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,
        PagIbigDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,
        WithholdingTax DECIMAL(18,2) NOT NULL DEFAULT 0,
        OtherDeductions DECIMAL(18,2) NOT NULL DEFAULT 0,
        Deductions DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Paid',
        PaymentMethod NVARCHAR(100) NOT NULL DEFAULT 'Bank Transfer',
        ProcessedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ProcessedBy NVARCHAR(100) NOT NULL DEFAULT 'Manager'
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PayrollRecords' AND COLUMN_NAME = 'SssDeduction')
    BEGIN
        ALTER TABLE PayrollRecords ADD SssDeduction DECIMAL(18,2) NOT NULL DEFAULT 0;
        ALTER TABLE PayrollRecords ADD PhilHealthDeduction DECIMAL(18,2) NOT NULL DEFAULT 0;
        ALTER TABLE PayrollRecords ADD PagIbigDeduction DECIMAL(18,2) NOT NULL DEFAULT 0;
        ALTER TABLE PayrollRecords ADD WithholdingTax DECIMAL(18,2) NOT NULL DEFAULT 0;
        ALTER TABLE PayrollRecords ADD OtherDeductions DECIMAL(18,2) NOT NULL DEFAULT 0;
    END
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsurePayrollSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPayroll(int companyId)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsurePayrollSchemaAsync(tenantDb, companyId);

                var records = await tenantDb.PayrollRecords
                    .AsNoTracking()
                    .Where(p => p.CompanyId == companyId)
                    .OrderByDescending(p => p.ProcessedAt)
                    .ToListAsync();

                return Ok(records);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPayroll error: {ex.Message}");
                return StatusCode(500, new { error = $"Failed to fetch payroll records: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayrollRecord(int companyId, [FromBody] PayrollRecord record)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            if (record == null)
            {
                return BadRequest(new { error = "Payroll record payload is required." });
            }

            if (record.BaseSalary < 0 || record.OvertimePay < 0 || record.CommissionAmount < 0 || record.OtherDeductions < 0)
            {
                return BadRequest(new { error = "Base salary, overtime pay, commission amount, and other deductions cannot be negative." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsurePayrollSchemaAsync(tenantDb, companyId);

                // Verify staff member exists and belongs to current tenant
                var staff = await tenantDb.StaffMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StaffId == record.StaffId && s.CompanyId == companyId);
                if (staff == null)
                {
                    return BadRequest(new { error = $"Staff member ID {record.StaffId} was not found or does not belong to company {companyId}." });
                }

                // Recalculate Philippine statutory deductions server-side so client cannot manipulate deductions/net pay
                var calc = PayrollCalculationService.Calculate(
                    record.BaseSalary,
                    record.OvertimePay,
                    record.CommissionAmount,
                    record.OtherDeductions);

                record.CompanyId = companyId;
                record.StaffName = staff.FullName;
                record.Role = staff.Role;
                record.SssDeduction = calc.SssDeduction;
                record.PhilHealthDeduction = calc.PhilHealthDeduction;
                record.PagIbigDeduction = calc.PagIbigDeduction;
                record.WithholdingTax = calc.WithholdingTax;
                record.OtherDeductions = calc.OtherDeductions;
                record.Deductions = calc.TotalDeductions;
                record.ProcessedAt = DateTime.UtcNow;

                var existing = await tenantDb.PayrollRecords
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StaffId == record.StaffId && p.PeriodStart == record.PeriodStart && p.PeriodEnd == record.PeriodEnd);
                if (existing != null)
                {
                    return Ok(existing);
                }

                tenantDb.PayrollRecords.Add(record);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPayroll), new { companyId }, record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save payroll record: {ex.Message}" });
            }
        }

        [HttpDelete("{payrollId:int}")]
        public async Task<IActionResult> DeletePayrollRecord(int companyId, int payrollId)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var record = await tenantDb.PayrollRecords.FirstOrDefaultAsync(p => p.PayrollId == payrollId && p.CompanyId == companyId);
                if (record == null)
                {
                    return NotFound(new { error = $"Payroll record {payrollId} not found for company {companyId}." });
                }

                tenantDb.PayrollRecords.Remove(record);
                await tenantDb.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to delete payroll record: {ex.Message}" });
            }
        }
    }
}
