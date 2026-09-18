using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;
using ERP.infrastructure.services;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class PayrollController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public PayrollController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsurePayrollSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
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
        Deductions DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Paid',
        PaymentMethod NVARCHAR(100) NOT NULL DEFAULT 'Bank Transfer',
        ProcessedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ProcessedBy NVARCHAR(100) NOT NULL DEFAULT 'Manager'
    );
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
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<PayrollRecord>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsurePayrollSchemaAsync(tenantDb, companyId);

                var records = await tenantDb.PayrollRecords
                    .AsNoTracking()
                    .OrderByDescending(p => p.ProcessedAt)
                    .ToListAsync();

                return Ok(records);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPayroll tenantDb error: {ex.Message}");
                return Ok(new List<PayrollRecord>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayrollRecord(int companyId, [FromBody] PayrollRecord record)
        {
            if (record == null)
            {
                return BadRequest(new { error = "Payroll record payload is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsurePayrollSchemaAsync(tenantDb, companyId);

                record.CompanyId = companyId;
                record.ProcessedAt = DateTime.UtcNow;

                tenantDb.PayrollRecords.Add(record);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPayroll), new { companyId }, record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save payroll record: {ex.Message}" });
            }
        }
    }
}
