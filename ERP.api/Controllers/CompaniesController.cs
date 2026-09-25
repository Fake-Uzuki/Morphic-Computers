using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.domain.entities;
using ERP.infrastructure.data;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly MasterErpDbContext _masterDb;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(MasterErpDbContext masterDb, ILogger<CompaniesController> logger)
        {
            _masterDb = masterDb;
            _logger = logger;
        }

        public record PlanUpgradeRequest(string PlanName);

        /// <summary>
        /// Retrieves all registered companies/tenants.
        /// Returns HTTP 503 Service Unavailable when the cloud Master database is unreachable.
        /// Does not return fake/demo companies.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _masterDb.Companies
                    .AsNoTracking()
                    .OrderBy(c => c.CompanyId)
                    .ToListAsync();

                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master database is currently unreachable when querying companies.");
                return StatusCode(503, new { error = "Master database is currently unavailable." });
            }
        }

        /// <summary>
        /// Retrieves company subscription details and plan permissions.
        /// Returns HTTP 503 Service Unavailable when the cloud Master database is unreachable.
        /// </summary>
        [HttpGet("{companyId:int}/subscription")]
        public async Task<IActionResult> GetSubscription(int companyId)
        {
            try
            {
                var company = await _masterDb.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return NotFound(new { error = $"Company ID {companyId} not found." });
                }

                return Ok(new
                {
                    company.CompanyId,
                    company.CompanyCode,
                    company.CompanyName,
                    company.PlanName,
                    company.IsPOSAllowed,
                    company.IsInventoryAllowed,
                    company.IsRepairAllowed,
                    company.IsSupplierAllowed,
                    company.IsBusinessIntelligenceAllowed,
                    company.IsPayrollAllowed,
                    company.IsBranchAllowed,
                    company.IsDashboardAllowed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master database is currently unreachable when retrieving subscription for company {CompanyId}.", companyId);
                return StatusCode(503, new { error = "Master database is currently unavailable." });
            }
        }

        /// <summary>
        /// Dynamically upgrades or changes a company's subscription plan.
        /// </summary>
        [HttpPost("{companyId:int}/upgrade-plan")]
        public async Task<IActionResult> UpgradePlan(int companyId, [FromBody] PlanUpgradeRequest request)
        {
            try
            {
                var company = await _masterDb.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return NotFound(new { error = $"Company ID {companyId} not found." });
                }

                company.PlanName = request.PlanName;
                await _masterDb.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Company plan updated to '{company.PlanName}' successfully.",
                    company.CompanyId,
                    company.CompanyName,
                    company.PlanName,
                    company.IsPOSAllowed,
                    company.IsInventoryAllowed,
                    company.IsRepairAllowed,
                    company.IsSupplierAllowed,
                    company.IsBusinessIntelligenceAllowed,
                    company.IsPayrollAllowed,
                    company.IsBranchAllowed,
                    company.IsDashboardAllowed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master database is currently unreachable when upgrading plan for company {CompanyId}.", companyId);
                return StatusCode(503, new { error = "Master database is currently unavailable." });
            }
        }
    }
}
