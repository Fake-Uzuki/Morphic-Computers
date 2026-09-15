using System.Threading.Tasks;
using ERP.infrastructure.data;

namespace ERP.infrastructure.services
{
    public interface ITenantDbContextFactory
    {
        Task<TenantErpDbContext> CreateAsync(int companyId);
    }
}
