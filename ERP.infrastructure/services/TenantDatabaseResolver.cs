using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ERP.infrastructure.data;

namespace ERP.infrastructure.services
{
    public class TenantDatabaseResolver : ITenantDatabaseResolver
    {
        private readonly MasterErpDbContext _masterDb;
        private readonly IConfiguration? _configuration;

        public TenantDatabaseResolver(MasterErpDbContext masterDb, IConfiguration? configuration = null)
        {
            _masterDb = masterDb;
            _configuration = configuration;
        }

        public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId)
        {
            // 1. Try resolving from the primary Master DB
            try
            {
                var tenantDatabase = await _masterDb.CompanyDatabases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.CompanyId == companyId &&
                        x.IsActive);

                if (tenantDatabase != null)
                {
                    return new TenantDatabaseInfo
                    {
                        ServerName = tenantDatabase.ServerName,
                        DatabaseName = tenantDatabase.DatabaseName,
                        CredentialKey = tenantDatabase.CredentialKey
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TenantDatabaseResolver primary DB query note: {ex.Message}");
            }

            // 2. Fallback to Local Master DB (e.g. for Tenant C and local-first development)
            string? localMasterConn = _configuration?.GetConnectionString("LocalMaster");
            if (string.IsNullOrWhiteSpace(localMasterConn))
            {
                localMasterConn = "Server=(localdb)\\MSSQLLocalDB;Database=ERP_Master_Local;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=5;";
            }

            try
            {
                var localOptions = new DbContextOptionsBuilder<MasterErpDbContext>()
                    .UseSqlServer(localMasterConn, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    })
                    .Options;
                await using var localMasterDb = new MasterErpDbContext(localOptions);

                var localMapping = await localMasterDb.CompanyDatabases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.CompanyId == companyId &&
                        x.IsActive);

                if (localMapping != null)
                {
                    return new TenantDatabaseInfo
                    {
                        ServerName = localMapping.ServerName,
                        DatabaseName = localMapping.DatabaseName,
                        CredentialKey = localMapping.CredentialKey
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TenantDatabaseResolver local master DB fallback note: {ex.Message}");
            }

            throw new InvalidOperationException($"No active database mapping found in Master DB for CompanyId {companyId}.");
        }
    }
}
