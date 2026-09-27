using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;
using ERP.infrastructure.services;
using ERP.domain.security;
using ERP.api.Helpers;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public ExpensesController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private static async Task EnsureExpensesSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'BranchId')
BEGIN
    ALTER TABLE Expenses ADD BranchId INT NULL;
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureExpensesSchema note: {ex.Message}");
            }
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            return await PlanAccessHelper.CheckPlanAccessAsync(
                _masterDb, companyId, ErpModule.FinancialStatements, "Financial Statements / Expenses", "Small or Medium");
        }

        private async Task<bool> IsPlanAllowedAsync(int companyId)
        {
            var access = await CheckPlanAccessAsync(companyId);
            return access.Allowed;
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenses(int companyId, [FromQuery] bool includeArchived = true, [FromQuery] int? branchId = null)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureExpensesSchemaAsync(tenantDb, companyId);

                if (branchId.HasValue)
                {
                    bool branchValid = await tenantDb.Branches.AnyAsync(b => b.BranchId == branchId.Value && b.CompanyId == companyId);
                    if (!branchValid)
                    {
                        return BadRequest(new { error = $"Branch ID {branchId.Value} does not belong to Company {companyId}." });
                    }
                }

                var query = tenantDb.Expenses.AsNoTracking().Where(e => e.CompanyId == companyId);
                if (!includeArchived)
                {
                    query = query.Where(e => e.IsActive);
                }
                if (branchId.HasValue)
                {
                    query = query.Where(e => e.BranchId == branchId.Value);
                }

                var list = await query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.ExpenseId).ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetExpenses error: {ex.Message}");
                return StatusCode(500, new { error = "Failed to load expenses from tenant database." });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExpenseById(int companyId, int id)
        {
            if (!await IsPlanAllowedAsync(companyId))
            {
                return StatusCode(403, new { error = "Plan does not include access to the Financial Statements / Expenses module." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureExpensesSchemaAsync(tenantDb, companyId);

                var expense = await tenantDb.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.ExpenseId == id && e.CompanyId == companyId);
                if (expense == null)
                {
                    return NotFound(new { error = $"Expense ID {id} not found for this tenant." });
                }
                return Ok(expense);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense(int companyId, [FromBody] ExpenseRecord record)
        {
            if (record == null)
            {
                return BadRequest(new { error = "Expense record payload is required." });
            }

            if (record.CompanyId != 0 && record.CompanyId != companyId)
            {
                return BadRequest(new { error = $"Cross-tenant expense creation rejected. Record company ID {record.CompanyId} does not match route company ID {companyId}." });
            }

            if (!await IsPlanAllowedAsync(companyId))
            {
                return StatusCode(403, new { error = "Plan does not include access to the Financial Statements / Expenses module." });
            }

            if (record.Amount <= 0)
            {
                return BadRequest(new { error = "Expense amount must be greater than zero." });
            }

            if (string.IsNullOrWhiteSpace(record.Description))
            {
                return BadRequest(new { error = "Description is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureExpensesSchemaAsync(tenantDb, companyId);

                if (record.BranchId.HasValue)
                {
                    bool branchValid = await tenantDb.Branches.AnyAsync(b => b.BranchId == record.BranchId.Value && b.CompanyId == companyId);
                    if (!branchValid)
                    {
                        return BadRequest(new { error = $"Cross-branch or unauthorized branch assignment rejected. Branch ID {record.BranchId.Value} does not belong to Company {companyId}." });
                    }
                }

                record.CompanyId = companyId;
                record.ExpenseId = 0;
                if (string.IsNullOrWhiteSpace(record.ExpenseNumber))
                {
                    record.ExpenseNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
                }
                if (record.ExpenseDate == default)
                {
                    record.ExpenseDate = DateTime.UtcNow;
                }
                record.CreatedAt = DateTime.UtcNow;

                tenantDb.Expenses.Add(record);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetExpenseById), new { companyId, id = record.ExpenseId }, record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateExpense(int companyId, int id, [FromBody] ExpenseRecord record)
        {
            if (record == null)
            {
                return BadRequest(new { error = "Expense record payload is required." });
            }

            if (record.CompanyId != 0 && record.CompanyId != companyId)
            {
                return BadRequest(new { error = $"Cross-tenant expense update rejected. Record company ID {record.CompanyId} does not match route company ID {companyId}." });
            }

            if (!await IsPlanAllowedAsync(companyId))
            {
                return StatusCode(403, new { error = "Plan does not include access to the Financial Statements / Expenses module." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureExpensesSchemaAsync(tenantDb, companyId);

                if (record.BranchId.HasValue)
                {
                    bool branchValid = await tenantDb.Branches.AnyAsync(b => b.BranchId == record.BranchId.Value && b.CompanyId == companyId);
                    if (!branchValid)
                    {
                        return BadRequest(new { error = $"Cross-branch or unauthorized branch assignment rejected. Branch ID {record.BranchId.Value} does not belong to Company {companyId}." });
                    }
                }

                var existing = await tenantDb.Expenses.FirstOrDefaultAsync(e => e.ExpenseId == id && e.CompanyId == companyId);
                if (existing == null)
                {
                    return NotFound(new { error = $"Expense ID {id} not found." });
                }

                existing.BranchId = record.BranchId;
                existing.Category = record.Category;
                existing.Description = record.Description;
                existing.Amount = record.Amount;
                existing.PaidTo = record.PaidTo;
                existing.PaymentMethod = record.PaymentMethod;
                existing.RecordedBy = record.RecordedBy;
                existing.ReceiptRef = record.ReceiptRef;
                existing.Notes = record.Notes;
                existing.IsTaxDeductible = record.IsTaxDeductible;
                existing.IsActive = record.IsActive;
                if (record.ExpenseDate != default)
                {
                    existing.ExpenseDate = record.ExpenseDate;
                }

                await tenantDb.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> ArchiveExpense(int companyId, int id)
        {
            if (!await IsPlanAllowedAsync(companyId))
            {
                return StatusCode(403, new { error = "Plan does not include access to the Financial Statements / Expenses module." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var existing = await tenantDb.Expenses.FirstOrDefaultAsync(e => e.ExpenseId == id && e.CompanyId == companyId);
                if (existing == null)
                {
                    return NotFound(new { error = $"Expense ID {id} not found." });
                }

                // Toggle archive status
                existing.IsActive = !existing.IsActive;
                await tenantDb.SaveChangesAsync();

                return Ok(new { message = $"Expense {existing.ExpenseNumber} archive status set to {!existing.IsActive}.", isActive = existing.IsActive });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
