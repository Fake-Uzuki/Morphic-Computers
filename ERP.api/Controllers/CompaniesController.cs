using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly MasterErpDbContext _masterDb;

        public CompaniesController(MasterErpDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        public record PlanUpgradeRequest(string PlanName);

        /// <summary>
        /// Retrieves all registered companies/tenants.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _masterDb.Companies
                .AsNoTracking()
                .OrderBy(c => c.CompanyId)
                .ToListAsync();

            if (!companies.Any())
            {
                // Fallback seed for demo
                companies = new List<Company>
                {
                    new Company { CompanyId = 1, CompanyCode = "TENANT_A", CompanyName = "Tenant A", PlanName = "Micro" },
                    new Company { CompanyId = 2, CompanyCode = "TENANT_B", CompanyName = "Tenant B", PlanName = "SmallBusiness" },
                    new Company { CompanyId = 3, CompanyCode = "TENANT_C", CompanyName = "Tenant C", PlanName = "Enterprise" }
                };
            }

            return Ok(companies);
        }

        /// <summary>
        /// Retrieves company subscription details and plan permissions.
        /// </summary>
        [HttpGet("{companyId:int}/subscription")]
        public async Task<IActionResult> GetSubscription(int companyId)
        {
            var company = await _masterDb.Companies.AsNoTracking()
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
                company.IsSupplierAllowed
            });
        }

        /// <summary>
        /// Dynamically upgrades or changes a company's subscription plan.
        /// </summary>
        [HttpPost("{companyId:int}/upgrade-plan")]
        public async Task<IActionResult> UpgradePlan(int companyId, [FromBody] PlanUpgradeRequest request)
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
                company.IsRepairAllowed,
                company.IsSupplierAllowed
            });
        }
    }
}
