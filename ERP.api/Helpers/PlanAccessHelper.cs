using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ERP.domain.entities;
using ERP.domain.security;
using ERP.infrastructure.data;

namespace ERP.api.Helpers
{
    /// <summary>
    /// Centralized helper for validating company plan module access with transient fault handling,
    /// graceful database exception recovery, and LocalMaster fallback for local-first/demo tenants.
    /// </summary>
    public static class PlanAccessHelper
    {
        public static async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(
            MasterErpDbContext masterDb,
            int companyId,
            ErpModule module,
            string moduleDisplayName,
            string requiredPlans = "Small or Medium",
            IConfiguration? configuration = null,
            ILogger? logger = null)
        {
            if (companyId <= 0)
            {
                return (false, "Super Admin or platform-level callers cannot perform tenant operations.", 403);
            }

            Company? company = null;
            bool primaryDbException = false;

            // 1. Try resolving company from primary Master DB (e.g. MonsterASP Cloud Master)
            try
            {
                company = await masterDb.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);
            }
            catch (Exception ex)
            {
                primaryDbException = true;
                logger?.LogWarning(ex, "Primary Master DB query failed for CompanyId {CompanyId} during plan check: {Message}", companyId, ex.Message);
                System.Diagnostics.Debug.WriteLine($"Primary Master DB note in CheckPlanAccessAsync: {ex.Message}");
            }

            // 2. If company was not found in primary DB or if primary DB had an error,
            // check LocalMaster fallback (e.g. for Tenant C and local demo tenants)
            if (company == null)
            {
                string? localMasterConn = configuration?.GetConnectionString("LocalMaster");
                if (string.IsNullOrWhiteSpace(localMasterConn))
                {
                    localMasterConn = "Server=(localdb)\\MSSQLLocalDB;Database=ERP_Master_Local;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=5;";
                }

                try
                {
                    var localOptions = new DbContextOptionsBuilder<MasterErpDbContext>()
                        .UseSqlServer(localMasterConn, sql => sql.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null))
                        .Options;

                    await using var localMasterDb = new MasterErpDbContext(localOptions);
                    company = await localMasterDb.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CompanyId == companyId);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Local Master DB query failed for CompanyId {CompanyId} during plan check: {Message}", companyId, ex.Message);
                    System.Diagnostics.Debug.WriteLine($"Local Master DB note in CheckPlanAccessAsync: {ex.Message}");
                }
            }

            // 3. Evaluate results
            if (company == null)
            {
                // If primary DB threw a connectivity/transient exception and Local DB also didn't have it
                if (primaryDbException)
                {
                    return (false, "Master database is currently unavailable.", 503);
                }

                // If master database was reachable and company record genuinely does not exist
                return (false, $"Company ID {companyId} not found.", 404);
            }

            // 4. Verify module entitlement using centralized ModuleAccessService
            if (!ModuleAccessService.IsModuleEnabled(company.PlanName, module))
            {
                return (false, $"Plan '{company.PlanName}' does not include access to the {moduleDisplayName} module. Upgrade to {requiredPlans} to enable {moduleDisplayName}.", 403);
            }

            return (true, null, 200);
        }

        public static async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(
            MasterErpDbContext masterDb,
            int companyId,
            string moduleName,
            string moduleDisplayName,
            string requiredPlans = "Small or Medium",
            IConfiguration? configuration = null,
            ILogger? logger = null)
        {
            if (ModuleAccessService.TryParseModule(moduleName, out ErpModule module))
            {
                return await CheckPlanAccessAsync(masterDb, companyId, module, moduleDisplayName, requiredPlans, configuration, logger);
            }

            return (false, $"Unknown module '{moduleName}'.", 400);
        }
    }
}
