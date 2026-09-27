using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.domain.security;
using ERP.infrastructure.data;
using ERP.infrastructure.services;

using ERP.api.Helpers;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public StaffController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            return await PlanAccessHelper.CheckPlanAccessAsync(
                _masterDb, companyId, ErpModule.Staff, "Staff Team", "Small or Medium");
        }

        private static async Task EnsureStaffSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffMembers')
BEGIN
    CREATE TABLE StaffMembers (
        StaffId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL DEFAULT 2,
        StaffCode NVARCHAR(50) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        Username NVARCHAR(100) NOT NULL,
        Role NVARCHAR(100) NOT NULL,
        PositionTitle NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        HourlyRate DECIMAL(18,2) NOT NULL DEFAULT 150.00,
        MonthlySalary DECIMAL(18,2) NOT NULL DEFAULT 25000.00,
        IsActive BIT NOT NULL DEFAULT 1,
        HiredDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StaffMembers' AND COLUMN_NAME = 'BranchId')
BEGIN
    ALTER TABLE StaffMembers ADD BranchId INT NULL;
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureStaffSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStaff(int companyId, [FromQuery] int? branchId = null)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<StaffMember>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureStaffSchemaAsync(tenantDb, companyId);

                if (branchId.HasValue)
                {
                    bool branchValid = await tenantDb.Branches.AnyAsync(b => b.BranchId == branchId.Value && b.CompanyId == companyId);
                    if (!branchValid)
                    {
                        return BadRequest(new { error = $"Branch ID {branchId.Value} does not belong to Company {companyId}." });
                    }
                }

                var query = tenantDb.StaffMembers.AsNoTracking().Where(s => s.CompanyId == companyId);
                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value);
                }

                var staff = await query
                    .OrderBy(s => s.FullName)
                    .ToListAsync();

                return Ok(staff);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetStaff tenantDb error: {ex.Message}");
                return Ok(new List<StaffMember>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(int companyId, [FromBody] StaffMember staff)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });

            if (companyId <= 0)
            {
                return BadRequest(new { error = "Super Admin / Platform context cannot create tenant operational staff." });
            }

            if (staff == null)
            {
                return BadRequest(new { error = "Staff payload is required." });
            }

            if (string.IsNullOrWhiteSpace(staff.FullName))
            {
                return BadRequest(new { error = "Please enter the staff member's full name." });
            }

            if (string.IsNullOrWhiteSpace(staff.Username))
            {
                return BadRequest(new { error = "Please enter a login username for this staff member." });
            }

            if (string.IsNullOrWhiteSpace(staff.Role))
            {
                return BadRequest(new { error = "Please select a system role for this staff member." });
            }

            if (staff.CompanyId != 0 && staff.CompanyId != companyId)
            {
                return BadRequest(new { error = $"Cross-tenant staff creation rejected. Staff company ID {staff.CompanyId} does not match route company ID {companyId}." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureStaffSchemaAsync(tenantDb, companyId);

                // Check for duplicate username within the company
                string trimmedUsername = staff.Username.Trim().ToLowerInvariant();
                bool usernameExists = await tenantDb.StaffMembers
                    .AnyAsync(s => s.CompanyId == companyId && s.Username.ToLower() == trimmedUsername);
                if (usernameExists)
                {
                    return BadRequest(new { error = $"The login username '{staff.Username}' is already assigned to another staff member in this company." });
                }

                if (staff.BranchId.HasValue && staff.BranchId.Value > 0)
                {
                    bool branchValid = await tenantDb.Branches.AnyAsync(b => b.BranchId == staff.BranchId.Value && b.CompanyId == companyId && b.IsActive);
                    if (!branchValid)
                    {
                        return BadRequest(new { error = $"Cross-branch or unauthorized branch assignment rejected. Branch ID {staff.BranchId.Value} does not belong to Company {companyId} or is not active." });
                    }
                }

                staff.CompanyId = companyId;
                staff.Username = staff.Username.Trim();
                staff.FullName = staff.FullName.Trim();
                if (string.IsNullOrWhiteSpace(staff.StaffCode))
                {
                    staff.StaffCode = $"EMP-{new Random().Next(1000, 9999)}";
                }

                // Check for existing record by StaffCode to avoid duplication during outbox sync
                var existing = await tenantDb.StaffMembers
                    .FirstOrDefaultAsync(s => s.StaffCode == staff.StaffCode && s.CompanyId == companyId);
                if (existing != null)
                {
                    existing.FullName = staff.FullName;
                    existing.Username = staff.Username;
                    existing.Role = staff.Role;
                    existing.PositionTitle = staff.PositionTitle;
                    existing.Email = staff.Email;
                    existing.PhoneNumber = staff.PhoneNumber;
                    existing.HourlyRate = staff.HourlyRate;
                    existing.MonthlySalary = staff.MonthlySalary;
                    existing.IsActive = staff.IsActive;
                    existing.BranchId = staff.BranchId;
                    await tenantDb.SaveChangesAsync();
                    return Ok(existing);
                }

                staff.HiredDate = DateTime.UtcNow;
                staff.IsActive = true;

                tenantDb.StaffMembers.Add(staff);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetStaff), new { companyId }, staff);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save staff member: {ex.Message}" });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStaff(int companyId, int id, [FromBody] StaffMember updated)
        {
            if (companyId <= 0)
            {
                return BadRequest(new { error = "Super Admin / Platform context cannot update tenant operational staff." });
            }

            if (updated.CompanyId != 0 && updated.CompanyId != companyId)
            {
                return BadRequest(new { error = $"Cross-tenant staff update rejected. Staff company ID {updated.CompanyId} does not match route company ID {companyId}." });
            }

            if (string.IsNullOrWhiteSpace(updated.FullName))
            {
                return BadRequest(new { error = "Please enter the staff member's full name." });
            }

            if (string.IsNullOrWhiteSpace(updated.Username))
            {
                return BadRequest(new { error = "Please enter a login username for this staff member." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureStaffSchemaAsync(tenantDb, companyId);

                // Prevent username collision with other staff members
                string trimmedUsername = updated.Username.Trim().ToLowerInvariant();
                bool usernameExists = await tenantDb.StaffMembers
                    .AnyAsync(s => s.CompanyId == companyId && s.StaffId != id && s.Username.ToLower() == trimmedUsername);
                if (usernameExists)
                {
                    return BadRequest(new { error = $"The login username '{updated.Username}' is already assigned to another staff member in this company." });
                }

                if (updated.BranchId.HasValue && updated.BranchId.Value > 0)
                {
                    bool branchValid = await tenantDb.Branches.AnyAsync(b => b.BranchId == updated.BranchId.Value && b.CompanyId == companyId && b.IsActive);
                    if (!branchValid)
                    {
                        return BadRequest(new { error = $"Cross-branch or unauthorized branch assignment rejected. Branch ID {updated.BranchId.Value} does not belong to Company {companyId} or is not active." });
                    }
                }

                var staff = await tenantDb.StaffMembers.FirstOrDefaultAsync(s => s.StaffId == id && s.CompanyId == companyId);
                if (staff == null)
                {
                    return NotFound(new { error = $"Staff ID {id} not found." });
                }

                staff.FullName = updated.FullName.Trim();
                staff.Username = updated.Username.Trim();
                staff.Role = updated.Role;
                staff.PositionTitle = updated.PositionTitle;
                staff.Email = updated.Email;
                staff.PhoneNumber = updated.PhoneNumber;
                staff.HourlyRate = updated.HourlyRate;
                staff.MonthlySalary = updated.MonthlySalary;
                staff.IsActive = updated.IsActive;
                staff.BranchId = updated.BranchId;

                int affected = await tenantDb.SaveChangesAsync();
                if (affected == 0 && !tenantDb.Entry(staff).State.HasFlag(EntityState.Unchanged))
                {
                    return StatusCode(500, new { error = "Update failed: 0 rows affected." });
                }
                return Ok(staff);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update staff member: {ex.Message}" });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateStaff(int companyId, int id)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureStaffSchemaAsync(tenantDb, companyId);

                var staff = await tenantDb.StaffMembers.FirstOrDefaultAsync(s => s.StaffId == id && s.CompanyId == companyId);
                if (staff == null)
                {
                    return NotFound(new { error = $"Staff ID {id} not found." });
                }

                staff.IsActive = false;
                await tenantDb.SaveChangesAsync();
                return Ok(new { message = "Staff member deactivated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to deactivate staff: {ex.Message}" });
            }
        }
    }
}
