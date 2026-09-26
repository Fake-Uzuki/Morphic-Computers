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

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;

        public BranchesController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            if (companyId <= 0)
            {
                return (false, "Super Admin or platform-level callers cannot perform tenant operational branch management.", 403);
            }

            var company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                return (false, $"Company ID {companyId} not found.", 404);
            }

            if (!ModuleAccessService.IsModuleEnabled(company.PlanName, "BranchManagement"))
            {
                return (false, $"Plan '{company.PlanName}' does not include access to the Branch Management module. Upgrade to Medium to enable Branch Management.", 403);
            }

            return (true, null, 200);
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches(int companyId, [FromQuery] bool includeArchived = true)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var query = tenantDb.Branches.AsNoTracking().Where(b => b.CompanyId == companyId);
                if (!includeArchived)
                {
                    query = query.Where(b => b.IsActive);
                }

                var list = await query.OrderBy(b => b.BranchCode).ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBranches error: {ex.Message}");
                return StatusCode(500, new { error = "Failed to load branches from tenant database." });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBranchById(int companyId, int id)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var branch = await tenantDb.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchId == id && b.CompanyId == companyId);
                if (branch == null)
                {
                    return NotFound(new { error = $"Branch ID {id} not found for this company." });
                }
                return Ok(branch);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBranch(int companyId, [FromBody] Branch branch)
        {
            if (branch == null)
            {
                return BadRequest(new { error = "Branch payload is required." });
            }

            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            if (string.IsNullOrWhiteSpace(branch.BranchName))
            {
                return BadRequest(new { error = "Branch name is required." });
            }

            if (string.IsNullOrWhiteSpace(branch.City))
            {
                return BadRequest(new { error = "City/Region is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);

                // Auto-generate branch code if not provided
                if (string.IsNullOrWhiteSpace(branch.BranchCode))
                {
                    int existingCount = await tenantDb.Branches.CountAsync(b => b.CompanyId == companyId);
                    branch.BranchCode = $"BR-{(existingCount + 1):D3}";
                }
                else
                {
                    branch.BranchCode = branch.BranchCode.Trim();
                }

                // Validate BranchCode uniqueness within the tenant
                bool codeExists = await tenantDb.Branches.AnyAsync(b => b.CompanyId == companyId && b.BranchCode == branch.BranchCode);
                if (codeExists)
                {
                    return Conflict(new { error = $"A branch with code '{branch.BranchCode}' already exists for this company." });
                }

                branch.CompanyId = companyId;
                branch.BranchId = 0;
                branch.CreatedAt = DateTime.UtcNow;

                tenantDb.Branches.Add(branch);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBranchById), new { companyId, id = branch.BranchId }, branch);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBranch(int companyId, int id, [FromBody] Branch branch)
        {
            if (branch == null)
            {
                return BadRequest(new { error = "Branch payload is required." });
            }

            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            if (string.IsNullOrWhiteSpace(branch.BranchName))
            {
                return BadRequest(new { error = "Branch name is required." });
            }

            if (string.IsNullOrWhiteSpace(branch.City))
            {
                return BadRequest(new { error = "City/Region is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var existing = await tenantDb.Branches.FirstOrDefaultAsync(b => b.BranchId == id && b.CompanyId == companyId);
                if (existing == null)
                {
                    return NotFound(new { error = $"Branch ID {id} not found." });
                }

                string newCode = string.IsNullOrWhiteSpace(branch.BranchCode) ? existing.BranchCode : branch.BranchCode.Trim();
                if (!string.Equals(existing.BranchCode, newCode, StringComparison.OrdinalIgnoreCase))
                {
                    bool codeExists = await tenantDb.Branches.AnyAsync(b => b.CompanyId == companyId && b.BranchId != id && b.BranchCode == newCode);
                    if (codeExists)
                    {
                        return Conflict(new { error = $"A branch with code '{newCode}' already exists for this company." });
                    }
                    existing.BranchCode = newCode;
                }

                existing.BranchName = branch.BranchName.Trim();
                existing.Address = branch.Address?.Trim() ?? string.Empty;
                existing.City = branch.City.Trim();
                existing.ContactNumber = branch.ContactNumber?.Trim() ?? string.Empty;
                existing.ManagerName = branch.ManagerName?.Trim() ?? string.Empty;
                existing.AssignedStaffCount = branch.AssignedStaffCount;
                existing.IsActive = branch.IsActive;

                await tenantDb.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> ArchiveBranch(int companyId, int id)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var existing = await tenantDb.Branches.FirstOrDefaultAsync(b => b.BranchId == id && b.CompanyId == companyId);
                if (existing == null)
                {
                    return NotFound(new { error = $"Branch ID {id} not found." });
                }

                existing.IsActive = !existing.IsActive;
                await tenantDb.SaveChangesAsync();

                return Ok(new { message = $"Branch {existing.BranchCode} active status set to {existing.IsActive}.", isActive = existing.IsActive });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
