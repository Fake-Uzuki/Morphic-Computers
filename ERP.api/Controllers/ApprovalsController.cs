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
    public class ApprovalsController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public ApprovalsController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            return await PlanAccessHelper.CheckPlanAccessAsync(
                _masterDb, companyId, ErpModule.Approvals, "Policies & Approvals", "Small or Medium");
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
        ResolvedAt DATETIME2 NULL,
        TargetReferenceId NVARCHAR(200) NULL
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApprovalRequests') AND name = 'TargetReferenceId')
    BEGIN
        ALTER TABLE ApprovalRequests ADD TargetReferenceId NVARCHAR(200) NULL;
    END
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
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });

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
                        ResolvedAt = r.ResolvedAt,
                        TargetReferenceId = r.TargetReferenceId
                    })
                    .ToListAsync();

                // Deduplicate multiple procurement requests for the same PO
                var deduplicated = new List<ApprovalRequest>();
                var seenPoNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var req in requests)
                {
                    string? poNumber = req.TargetReferenceId;
                    if (string.IsNullOrWhiteSpace(poNumber) && !string.IsNullOrWhiteSpace(req.Title))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(req.Title, @"(PO-[\w-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success) poNumber = match.Groups[1].Value;
                    }

                    if (!string.IsNullOrWhiteSpace(poNumber) && 
                        (string.Equals(req.RequestType, "ProcurementRequest", StringComparison.OrdinalIgnoreCase) || poNumber.StartsWith("PO-", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (seenPoNumbers.Contains(poNumber))
                        {
                            continue; // Skip duplicate
                        }
                        seenPoNumbers.Add(poNumber);
                        req.TargetReferenceId = poNumber;
                    }

                    deduplicated.Add(req);
                }

                return Ok(deduplicated);
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

                // Prevent duplicate requests by RequestNumber
                var existing = await tenantDb.ApprovalRequests
                    .FirstOrDefaultAsync(r => r.RequestNumber == request.RequestNumber && r.CompanyId == companyId);
                if (existing != null)
                {
                    return Ok(existing);
                }

                // Prevent duplicate procurement approval requests by TargetReferenceId or Title
                string? targetRef = request.TargetReferenceId;
                if (string.IsNullOrWhiteSpace(targetRef) && !string.IsNullOrWhiteSpace(request.Title))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(request.Title, @"(PO-[\w-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success) targetRef = m.Groups[1].Value;
                }

                if (!string.IsNullOrWhiteSpace(targetRef))
                {
                    var existingTarget = await tenantDb.ApprovalRequests
                        .FirstOrDefaultAsync(r => r.CompanyId == companyId &&
                            (r.TargetReferenceId == targetRef ||
                             (r.Title != null && r.Title.Contains(targetRef))));
                    if (existingTarget != null)
                    {
                        return Ok(existingTarget);
                    }
                    request.TargetReferenceId = targetRef;
                }

                request.CreatedAt = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(request.Status)) request.Status = "Pending";

                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ApprovalRequests (
                        CompanyId, RequestNumber, RequestType, Title, ReasonDescription,
                        RequestedBy, RequestedAmount, Status, CreatedAt, TargetReferenceId
                    ) VALUES (
                        {request.CompanyId}, {request.RequestNumber}, {request.RequestType}, {request.Title}, {request.ReasonDescription},
                        {request.RequestedBy}, {request.RequestedAmount}, {request.Status}, {request.CreatedAt}, {request.TargetReferenceId}
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

                var existing = await tenantDb.ApprovalRequests.FirstOrDefaultAsync(r => r.RequestId == id);
                if (existing == null)
                {
                    return NotFound(new { error = $"Approval request ID {id} not found." });
                }

                DateTime resolvedAt = DateTime.UtcNow;
                existing.Status = dto.Status;
                existing.ReviewedBy = dto.ReviewedBy;
                existing.ReviewNotes = dto.ReviewNotes;
                existing.ResolvedAt = resolvedAt;

                // Sync linked Purchase Order if request is a Procurement Request
                string? poNum = existing.TargetReferenceId;
                if (string.IsNullOrWhiteSpace(poNum) && !string.IsNullOrWhiteSpace(existing.Title))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(existing.Title, @"(PO-[\w-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success) poNum = match.Groups[1].Value;
                }

                if (!string.IsNullOrEmpty(poNum) || string.Equals(existing.RequestType, "ProcurementRequest", StringComparison.OrdinalIgnoreCase))
                {
                    var po = await tenantDb.PurchaseOrders.FirstOrDefaultAsync(p =>
                        p.CompanyId == companyId &&
                        ((!string.IsNullOrEmpty(poNum) && (p.PurchaseOrderNumber == poNum || p.PurchaseOrderId.ToString() == poNum)) ||
                         (!string.IsNullOrEmpty(existing.TargetReferenceId) && (p.PurchaseOrderNumber == existing.TargetReferenceId || p.PurchaseOrderId.ToString() == existing.TargetReferenceId))));

                    if (po != null)
                    {
                        if (dto.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                        {
                            po.Status = "In Transit";
                            po.ApprovedBy = dto.ReviewedBy;
                        }
                        else if (dto.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                        {
                            po.Status = "Rejected";
                            po.ApprovedBy = $"Rejected by {dto.ReviewedBy}";
                        }
                        po.UpdatedAt = resolvedAt;
                    }
                }

                await tenantDb.SaveChangesAsync();

                return Ok(new { success = true, requestId = id, dto.Status, dto.ReviewedBy, dto.ReviewNotes, resolvedAt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to resolve approval request: {ex.Message}" });
            }
        }
    }
}
