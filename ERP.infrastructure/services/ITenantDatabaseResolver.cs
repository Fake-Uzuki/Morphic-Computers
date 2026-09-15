using System.Threading.Tasks;

namespace ERP.infrastructure.services
{
    public interface ITenantDatabaseResolver
    {
        Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);
    }
}
