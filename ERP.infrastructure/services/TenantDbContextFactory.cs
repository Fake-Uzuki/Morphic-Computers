using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ERP.infrastructure.data;

namespace ERP.infrastructure.services
{
    public class TenantDbContextFactory : ITenantDbContextFactory
    {
        private readonly ITenantDatabaseResolver _resolver;
        private readonly IConfiguration _configuration;

        public TenantDbContextFactory(
            ITenantDatabaseResolver resolver,
            IConfiguration configuration)
        {
            _resolver = resolver;
            _configuration = configuration;
        }

        public async Task<TenantErpDbContext> CreateAsync(int companyId)
        {
            var databaseInfo = await _resolver.GetDatabaseInfoAsync(companyId);

            string connectionString;
            if (string.Equals(databaseInfo.CredentialKey, "LocalTrusted", StringComparison.OrdinalIgnoreCase) ||
                databaseInfo.ServerName.Contains("localdb", StringComparison.OrdinalIgnoreCase))
            {
                connectionString =
                    $"Server={databaseInfo.ServerName};" +
                    $"Database={databaseInfo.DatabaseName};" +
                    $"Trusted_Connection=True;" +
                    $"Encrypt=False;" +
                    $"TrustServerCertificate=True;" +
                    $"MultipleActiveResultSets=True;" +
                    $"Connect Timeout=5;";
            }
            else
            {
                var userId = _configuration[
                    $"TenantCredentials:{databaseInfo.CredentialKey}:UserId"];

                var password = _configuration[
                    $"TenantCredentials:{databaseInfo.CredentialKey}:Password"];

                if (string.IsNullOrWhiteSpace(userId) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        $"Tenant credentials for '{databaseInfo.CredentialKey}' are not configured in application settings.");
                }

                connectionString =
                    $"Server={databaseInfo.ServerName};" +
                    $"Database={databaseInfo.DatabaseName};" +
                    $"User Id={userId};" +
                    $"Password={password};" +
                    $"Encrypt=False;" +
                    $"TrustServerCertificate=True;" +
                    $"MultipleActiveResultSets=True;" +
                    $"Connect Timeout=30;";
            }

            var options = new DbContextOptionsBuilder<TenantErpDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new TenantErpDbContext(options);
        }
    }
}
