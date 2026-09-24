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
    public class ApprovalsController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public ApprovalsController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsureApprovalsSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApprovalRequests')
BEGIN
    CREATE TABLE ApprovalRequests (
        RequestId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL DEFAULT 2,
        RequestNumber NVARCHAR(50) NOT NULL,
        RequestType NVARCHAR(100) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        ReasonDescription NVARCHAR(1000) NOT NULL,
        RequestedBy NVARCHAR(100) NOT NULL,
        RequestedAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        ReviewedBy NVARCHAR(100) NULL,
        ReviewNotes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ResolvedAt DATETIME2 NULL
    );
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureApprovalsSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests(int companyId, [FromQuery] string? status)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<ApprovalRequest>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureApprovalsSchemaAsync(tenantDb, companyId);

                var query = tenantDb.ApprovalRequests.AsNoTracking();
                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    query = query.Where(r => r.Status == status);
                }

                var requests = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ApprovalRequest
                    {
                        RequestId = r.RequestId,
                        CompanyId = r.CompanyId,
                        RequestNumber = r.RequestNumber,
                        RequestType = r.RequestType,
                        Title = r.Title,
                        ReasonDescription = r.ReasonDescription,
                        RequestedBy = r.RequestedBy,
                        RequestedAmount = r.RequestedAmount,
                        Status = r.Status,
                        ReviewedBy = r.ReviewedBy,
                        ReviewNotes = r.ReviewNotes,
                        CreatedAt = r.CreatedAt,
                        ResolvedAt = r.ResolvedAt
                    })
                    .ToListAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRequests tenantDb error: {ex.Message}");
                return Ok(new List<ApprovalRequest>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest(int companyId, [FromBody] ApprovalRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Approval request payload is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureApprovalsSchemaAsync(tenantDb, companyId);

                request.CompanyId = companyId;
                if (string.IsNullOrWhiteSpace(request.RequestNumber))
                {
                    request.RequestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
                }
                request.CreatedAt = DateTime.UtcNow;
                request.Status = "Pending";

                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ApprovalRequests (
                        CompanyId, RequestNumber, RequestType, Title, ReasonDescription,
                        RequestedBy, RequestedAmount, Status, CreatedAt
                    ) VALUES (
                        {request.CompanyId}, {request.RequestNumber}, {request.RequestType}, {request.Title}, {request.ReasonDescription},
                        {request.RequestedBy}, {request.RequestedAmount}, {request.Status}, {request.CreatedAt}
                    );
                ");

                int newId = await tenantDb.ApprovalRequests
                    .Where(r => r.RequestNumber == request.RequestNumber)
                    .Select(r => r.RequestId)
                    .FirstAsync();
                request.RequestId = newId;

                return CreatedAtAction(nameof(GetRequests), new { companyId }, request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to create approval request: {ex.Message}" });
            }
        }

        public record ResolveRequestDto(string Status, string ReviewedBy, string? ReviewNotes);

        [HttpPut("{id:int}/resolve")]
        public async Task<IActionResult> ResolveRequest(int companyId, int id, [FromBody] ResolveRequestDto dto)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureApprovalsSchemaAsync(tenantDb, companyId);

                bool exists = await tenantDb.ApprovalRequests.AnyAsync(r => r.RequestId == id);
                if (!exists)
                {
                    return NotFound(new { error = $"Approval request ID {id} not found." });
                }

                DateTime resolvedAt = DateTime.UtcNow;
                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE ApprovalRequests
                    SET Status = {dto.Status},
                        ReviewedBy = {dto.ReviewedBy},
                        ReviewNotes = {dto.ReviewNotes},
                        ResolvedAt = {resolvedAt}
                    WHERE RequestId = {id};
                ");

                return Ok(new { success = true, requestId = id, dto.Status, dto.ReviewedBy, dto.ReviewNotes, resolvedAt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to resolve approval request: {ex.Message}" });
            }
        }
    }
}
