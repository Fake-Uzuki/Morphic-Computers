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
    public class RepairsController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public RepairsController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsureRepairsSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RepairTickets')
BEGIN
    CREATE TABLE RepairTickets (
        RepairTicketId INT IDENTITY(1,1) PRIMARY KEY,
        TicketNumber NVARCHAR(50) NOT NULL,
        CompanyId INT NOT NULL DEFAULT 2,
        CustomerName NVARCHAR(200) NOT NULL,
        CustomerPhone NVARCHAR(50) NULL,
        CustomerEmail NVARCHAR(100) NULL,
        DeviceType NVARCHAR(100) NOT NULL,
        DeviceBrandModel NVARCHAR(200) NOT NULL,
        SerialNumber NVARCHAR(100) NULL,
        ReportedIssue NVARCHAR(1000) NOT NULL,
        DiagnosticNotes NVARCHAR(2000) NULL,
        AssignedTechnician NVARCHAR(100) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Received',
        LaborFee DECIMAL(18,2) NOT NULL DEFAULT 0,
        PartsCost DECIMAL(18,2) NOT NULL DEFAULT 0,
        DepositAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        EstimatedCompletionDate DATETIME2 NULL,
        CompletedAt DATETIME2 NULL,
        WarrantyTerms NVARCHAR(500) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureRepairsSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRepairs(int companyId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<RepairTicket>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureRepairsSchemaAsync(tenantDb, companyId);

                var tickets = await tenantDb.RepairTickets
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRepairs tenantDb error: {ex.Message}");
                return Ok(new List<RepairTicket>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepairTicket(int companyId, [FromBody] RepairTicket ticket)
        {
            if (ticket == null)
            {
                return BadRequest(new { error = "Repair ticket payload is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureRepairsSchemaAsync(tenantDb, companyId);

                ticket.CompanyId = companyId;
                if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
                {
                    ticket.TicketNumber = $"REP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                }
                ticket.CreatedAt = DateTime.UtcNow;
                ticket.IsActive = true;

                tenantDb.RepairTickets.Add(ticket);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRepairs), new { companyId }, ticket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save repair ticket: {ex.Message}" });
            }
        }

        public record UpdateStatusDto(string Status, string? DiagnosticNotes, string? AssignedTechnician);

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateRepairStatus(int companyId, int id, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureRepairsSchemaAsync(tenantDb, companyId);

                var ticket = await tenantDb.RepairTickets.FirstOrDefaultAsync(t => t.RepairTicketId == id);
                if (ticket == null)
                {
                    return NotFound(new { error = $"Repair ticket ID {id} not found." });
                }

                ticket.Status = dto.Status;
                if (!string.IsNullOrEmpty(dto.DiagnosticNotes))
                {
                    ticket.DiagnosticNotes = dto.DiagnosticNotes;
                }
                if (!string.IsNullOrEmpty(dto.AssignedTechnician))
                {
                    ticket.AssignedTechnician = dto.AssignedTechnician;
                }
                if (dto.Status == "Completed")
                {
                    ticket.CompletedAt = DateTime.UtcNow;
                }

                await tenantDb.SaveChangesAsync();
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update repair status: {ex.Message}" });
            }
        }

        public record UpdateBillingDto(decimal LaborFee, decimal PartsCost, decimal DepositAmount);

        [HttpPut("{id:int}/billing")]
        public async Task<IActionResult> UpdateRepairBilling(int companyId, int id, [FromBody] UpdateBillingDto dto)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureRepairsSchemaAsync(tenantDb, companyId);

                var ticket = await tenantDb.RepairTickets.FirstOrDefaultAsync(t => t.RepairTicketId == id);
                if (ticket == null)
                {
                    return NotFound(new { error = $"Repair ticket ID {id} not found." });
                }

                ticket.LaborFee = dto.LaborFee;
                ticket.PartsCost = dto.PartsCost;
                ticket.DepositAmount = dto.DepositAmount;

                await tenantDb.SaveChangesAsync();
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update repair billing: {ex.Message}" });
            }
        }
    }
}
