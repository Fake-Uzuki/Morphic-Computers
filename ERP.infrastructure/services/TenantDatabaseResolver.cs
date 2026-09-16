using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.infrastructure.data;

namespace ERP.infrastructure.services
{
    public class TenantDatabaseResolver : ITenantDatabaseResolver
    {
        private readonly MasterErpDbContext _masterDb;

        public TenantDatabaseResolver(MasterErpDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId)
        {
            try
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
                    var tenantDatabase = await _masterDb.CompanyDatabases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.CompanyId == companyId &&
                            x.IsActive, cts.Token);

                    if (tenantDatabase != null)
                    {
                        Console.WriteLine($"[RESOLVER] Found in Master DB: CompanyId={companyId}, Server={tenantDatabase.ServerName}, DB={tenantDatabase.DatabaseName}, CredKey={tenantDatabase.CredentialKey}");
                        return new TenantDatabaseInfo
                        {
                            ServerName = tenantDatabase.ServerName,
                            DatabaseName = tenantDatabase.DatabaseName,
                            CredentialKey = tenantDatabase.CredentialKey
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Master DB resolution note: {ex.Message}. Using resilient tenant mapping.");
            }

            // Fallback default routing (Company 1 -> TenantA, Company 2 -> TenantB)
            string server = companyId == 2 ? "db66562.public.databaseasp.net" : "db67673.public.databaseasp.net";
            string dbName = companyId == 2 ? "db66562" : "db67673";
            string credKey = companyId == 2 ? "TenantB" : "TenantA";

            return new TenantDatabaseInfo
            {
                ServerName = server,
                DatabaseName = dbName,
                CredentialKey = credKey
            };
        }
    }
}
