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
    public class PoliciesController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public PoliciesController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsurePoliciesSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StorePolicies')
BEGIN
    CREATE TABLE StorePolicies (
        PolicyId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL DEFAULT 2,
        PolicyType NVARCHAR(100) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        ContentText NVARCHAR(MAX) NOT NULL,
        LastUpdatedBy NVARCHAR(100) NOT NULL DEFAULT 'Admin',
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsurePoliciesSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicies(int companyId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<StorePolicy>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsurePoliciesSchemaAsync(tenantDb, companyId);

                var policies = await tenantDb.StorePolicies
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(policies);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPolicies tenantDb error: {ex.Message}");
                return Ok(new List<StorePolicy>());
            }
        }

        public record UpdatePolicyDto(string ContentText, string UpdatedBy);

        [HttpPut("{policyType}")]
        public async Task<IActionResult> UpdatePolicy(int companyId, string policyType, [FromBody] UpdatePolicyDto dto)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsurePoliciesSchemaAsync(tenantDb, companyId);

                var policy = await tenantDb.StorePolicies.FirstOrDefaultAsync(p => p.PolicyType == policyType);
                if (policy == null)
                {
                    policy = new StorePolicy
                    {
                        CompanyId = companyId,
                        PolicyType = policyType,
                        Title = policyType,
                        ContentText = dto.ContentText,
                        LastUpdatedBy = dto.UpdatedBy,
                        UpdatedAt = DateTime.UtcNow
                    };
                    tenantDb.StorePolicies.Add(policy);
                }
                else
                {
                    policy.ContentText = dto.ContentText;
                    policy.LastUpdatedBy = dto.UpdatedBy;
                    policy.UpdatedAt = DateTime.UtcNow;
                }

                await tenantDb.SaveChangesAsync();
                return Ok(policy);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update policy: {ex.Message}" });
            }
        }
    }
}
