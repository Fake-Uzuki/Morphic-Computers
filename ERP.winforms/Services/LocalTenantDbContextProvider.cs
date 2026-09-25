using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;
using ERP.infrastructure.services;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Factory for creating local TenantErpDbContext instances by dynamically resolving
    /// tenant connection info from ERP_Master_Local.CompanyDatabases via TenantDatabaseResolver.
    /// Does not contain hardcoded company-to-database mappings.
    /// </summary>
    public static class LocalTenantDbContextProvider
    {
        public const string LocalMasterConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=ERP_Master_Local;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=5;";

        /// <summary>
        /// Creates a MasterErpDbContext instance targeting ERP_Master_Local.
        /// </summary>
        public static MasterErpDbContext CreateMasterDbContext()
        {
            var options = new DbContextOptionsBuilder<MasterErpDbContext>()
                .UseSqlServer(LocalMasterConnectionString)
                .Options;

            return new MasterErpDbContext(options);
        }

        /// <summary>
        /// Dynamically resolves a company from ERP_Master_Local.CompanyDatabases by CompanyName or CompanyCode.
        /// Ensures the company exists and has a configured local database mapping.
        /// </summary>
        public static async Task<Company?> ResolveCompanyAsync(string companyInput)
        {
            if (string.IsNullOrWhiteSpace(companyInput)) return null;

            string normalized = companyInput.Trim().ToLower();
            await using var masterDb = CreateMasterDbContext();
            return await (from c in masterDb.Companies.AsNoTracking()
                          join cd in masterDb.CompanyDatabases.AsNoTracking() on c.CompanyId equals cd.CompanyId
                          where c.CompanyName.ToLower() == normalized ||
                                c.CompanyCode.ToLower() == normalized
                          select c).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Dynamically resolves tenant database connection info from ERP_Master_Local.CompanyDatabases.
        /// Queries the local Master DB directly through TenantDatabaseResolver without hardcoded branching.
        /// </summary>
        public static async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId)
        {
            await using var masterDb = CreateMasterDbContext();
            var resolver = new TenantDatabaseResolver(masterDb);
            return await resolver.GetDatabaseInfoAsync(companyId).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates and returns a TenantErpDbContext instance configured for the resolved local tenant database.
        /// Uses Windows Authentication (Trusted_Connection=True) with no cloud credentials.
        /// </summary>
        public static async Task<TenantErpDbContext> CreateTenantDbContextAsync(int companyId)
        {
            var dbInfo = await GetDatabaseInfoAsync(companyId).ConfigureAwait(false);

            string connectionString =
                $"Server={dbInfo.ServerName};" +
                $"Database={dbInfo.DatabaseName};" +
                $"Trusted_Connection=True;" +
                $"Encrypt=False;" +
                $"TrustServerCertificate=True;" +
                $"MultipleActiveResultSets=True;" +
                $"Connect Timeout=5;";

            var options = new DbContextOptionsBuilder<TenantErpDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new TenantErpDbContext(options);
        }
    }
}
