using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Local database service providing read access to local tenant SQL Server databases.
    /// Operates against (localdb)\MSSQLLocalDB using dynamic resolution from LocalTenantDbContextProvider.
    /// In Phase 2C Steps 1-4, this service provides read-only operations using AsNoTracking.
    /// </summary>
    public class LocalDatabaseService
    {
        private static LocalDatabaseService? _instance;
        public static LocalDatabaseService Instance => _instance ??= new LocalDatabaseService();

        public LocalDatabaseService()
        {
        }

        /// <summary>
        /// Retrieves all categories for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Category>> GetCategoriesAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all products for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Product>> GetProductsAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var products = await context.Products
                .AsNoTracking()
                .OrderBy(p => p.ProductName)
                .ToListAsync()
                .ConfigureAwait(false);

            var inventories = await context.Inventories
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var p in products)
            {
                var inv = inventories.FirstOrDefault(i => i.ProductId == p.ProductId);
                if (inv != null)
                {
                    p.StockQuantity = (int)inv.QuantityOnHand;
                }
            }

            return products;
        }

        /// <summary>
        /// Retrieves all orders for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Order>> GetOrdersAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all customers for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Customer>> GetCustomersAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.Customers
                .AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all suppliers for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Supplier>> GetSuppliersAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.SupplierName)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all repair tickets for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<RepairTicket>> GetRepairsAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.RepairTickets
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all staff members for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<StaffMember>> GetStaffAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.StaffMembers
                .AsNoTracking()
                .OrderBy(s => s.FullName)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all payroll records for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<PayrollRecord>> GetPayrollAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.PayrollRecords
                .AsNoTracking()
                .OrderByDescending(p => p.ProcessedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all approval requests for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<ApprovalRequest>> GetApprovalsAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.ApprovalRequests
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all store policies for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<StorePolicy>> GetPoliciesAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.StorePolicies
                .AsNoTracking()
                .OrderBy(p => p.PolicyType)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all inventories for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Inventory>> GetInventoriesAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.Inventories
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }
}
