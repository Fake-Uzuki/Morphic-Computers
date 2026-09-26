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
        /// Retrieves all expenses for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<ExpenseRecord>> GetExpensesAsync(int companyId, bool includeArchived = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var query = context.Expenses.AsNoTracking().Where(e => e.CompanyId == companyId);
            if (!includeArchived)
            {
                query = query.Where(e => e.IsActive);
            }
            return await query
                .OrderByDescending(e => e.ExpenseDate)
                .ThenByDescending(e => e.ExpenseId)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all branches for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<Branch>> GetBranchesAsync(int companyId, bool includeArchived = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var query = context.Branches.AsNoTracking().Where(b => b.CompanyId == companyId);
            if (!includeArchived)
            {
                query = query.Where(b => b.IsActive);
            }
            return await query
                .OrderBy(b => b.BranchCode)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all purchase orders for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(int companyId, bool includeArchived = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var query = context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.CompanyId == companyId);

            if (!includeArchived)
            {
                query = query.Where(p => p.IsActive);
            }

            return await query
                .OrderByDescending(p => p.OrderDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a purchase order by ID for the specified tenant from the local tenant database.
        /// </summary>
        public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int companyId, int purchaseOrderId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId && p.CompanyId == companyId)
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

        // =========================================================================
        // OFFLINE WRITES & TRANSACTION-SAFE LOCAL OPERATIONS
        // =========================================================================

        /// <summary>
        /// Atomically saves an order and deducts inventory quantities in local SQL Server.
        /// Also enqueues a SyncOutbox record if enqueueSync is true.
        /// If inventory deduction or order save fails, transaction is rolled back.
        /// </summary>
        public async Task<bool> ProcessOrderAsync(int companyId, Order order, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                order.CompanyId = companyId;
                if (string.IsNullOrWhiteSpace(order.ItemsJson) || order.ItemsJson == "[]")
                {
                    order.ItemsJson = System.Text.Json.JsonSerializer.Serialize(order.Items);
                }

                bool exists = await context.Orders.AnyAsync(o => o.Id == order.Id).ConfigureAwait(false);
                if (!exists)
                {
                    // Validate items and stock availability before processing
                    foreach (var item in order.Items)
                    {
                        var prod = await context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId).ConfigureAwait(false);
                        if (prod == null)
                        {
                            throw new InvalidOperationException($"Product ID {item.ProductId} does not exist.");
                        }
                        if (!prod.IsActive)
                        {
                            throw new InvalidOperationException($"Product '{prod.ProductName}' is inactive/archived and cannot be sold.");
                        }

                        var inv = await context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId).ConfigureAwait(false);
                        int currentStock = (int)(inv?.QuantityOnHand ?? 0);
                        if (currentStock <= 0)
                        {
                            throw new InvalidOperationException($"Product '{prod.ProductName}' is out of stock (Stock: 0).");
                        }
                        if (currentStock < item.Quantity)
                        {
                            throw new InvalidOperationException($"Insufficient stock for '{prod.ProductName}'. Requested: {item.Quantity}, Available: {currentStock}.");
                        }
                    }

                    context.Orders.Add(order);
                }

                // Atomic inventory deduction
                foreach (var item in order.Items)
                {
                    var inv = await context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId).ConfigureAwait(false);
                    if (inv != null)
                    {
                        inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                        inv.LastUpdatedAt = DateTime.UtcNow;
                    }
                }

                if (enqueueSync)
                {
                    var outbox = new SyncOutboxItem
                    {
                        SyncId = Guid.NewGuid().ToString("N"),
                        CompanyId = companyId,
                        EntityType = "Order",
                        EntityId = order.Id,
                        Operation = "Create",
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(order),
                        CreatedAt = DateTime.UtcNow,
                        SyncStatus = "Pending"
                    };
                    context.SyncOutbox.Add(outbox);
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"LocalDatabaseService ProcessOrderAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VoidOrderAsync(int companyId, string orderId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                string cleanId = orderId.TrimStart('#');
                var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == cleanId || o.Id == orderId).ConfigureAwait(false);
                if (order == null) return false;

                order.Status = "Voided";
                order.ArchivedAt = DateTime.UtcNow;

                // Restock items in inventory
                if (!string.IsNullOrWhiteSpace(order.ItemsJson))
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(order.ItemsJson);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var inv = await context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId).ConfigureAwait(false);
                            if (inv != null)
                            {
                                inv.QuantityOnHand += item.Quantity;
                                inv.LastUpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"LocalDatabaseService VoidOrderAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreOrderAsync(int companyId, string orderId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                string cleanId = orderId.TrimStart('#');
                var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == cleanId || o.Id == orderId).ConfigureAwait(false);
                if (order == null) return false;

                order.Status = "Completed";
                order.ArchivedAt = null;

                // Re-deduct items from inventory
                if (!string.IsNullOrWhiteSpace(order.ItemsJson))
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(order.ItemsJson);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var inv = await context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId).ConfigureAwait(false);
                            if (inv != null)
                            {
                                inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                                inv.LastUpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"LocalDatabaseService RestoreOrderAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves or updates a product and updates its inventory quantity in local SQL Server.
        /// </summary>
        public async Task<Product> SaveProductAsync(int companyId, Product product, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                var existing = await context.Products.FirstOrDefaultAsync(p =>
                    (product.ProductId > 0 && p.ProductId == product.ProductId) ||
                    p.ProductCode == product.ProductCode).ConfigureAwait(false);

                bool isUpdate = existing != null;
                if (existing != null)
                {
                    existing.ProductName = product.ProductName;
                    existing.ProductCode = product.ProductCode;
                    existing.UnitPrice = product.UnitPrice;
                    existing.CategoryName = product.CategoryName;
                    existing.Description = product.Description;
                    existing.IsActive = product.IsActive;
                    existing.SupplierName = product.SupplierName;
                    existing.SupplierId = product.SupplierId;

                    var inv = await context.Inventories.FirstOrDefaultAsync(i => i.ProductId == existing.ProductId).ConfigureAwait(false);
                    if (inv != null)
                    {
                        inv.QuantityOnHand = product.StockQuantity;
                        inv.LastUpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        context.Inventories.Add(new Inventory
                        {
                            ProductId = existing.ProductId,
                            QuantityOnHand = product.StockQuantity,
                            ReorderLevel = 3,
                            LastUpdatedAt = DateTime.UtcNow
                        });
                    }

                    product.ProductId = existing.ProductId;
                }
                else
                {
                    product.ProductId = 0;
                    context.Products.Add(product);
                    await context.SaveChangesAsync().ConfigureAwait(false);

                    context.Inventories.Add(new Inventory
                    {
                        ProductId = product.ProductId,
                        QuantityOnHand = product.StockQuantity,
                        ReorderLevel = 3,
                        LastUpdatedAt = DateTime.UtcNow
                    });
                }

                if (enqueueSync)
                {
                    context.SyncOutbox.Add(new SyncOutboxItem
                    {
                        SyncId = Guid.NewGuid().ToString("N"),
                        CompanyId = companyId,
                        EntityType = "Product",
                        EntityId = product.ProductCode,
                        Operation = isUpdate ? "Update" : "Create",
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(product),
                        CreatedAt = DateTime.UtcNow,
                        SyncStatus = "Pending"
                    });
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return product;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"LocalDatabaseService SaveProductAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ArchiveProductAsync(int companyId, int productId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var prod = await context.Products.FirstOrDefaultAsync(p => p.ProductId == productId).ConfigureAwait(false);
            if (prod == null) return false;

            prod.IsActive = false;
            prod.ArchivedAt = DateTime.UtcNow;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Product",
                    EntityId = productId.ToString(),
                    Operation = "Archive",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { ProductId = productId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        public async Task<bool> RestoreProductAsync(int companyId, int productId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var prod = await context.Products.FirstOrDefaultAsync(p => p.ProductId == productId).ConfigureAwait(false);
            if (prod == null) return false;

            prod.IsActive = true;
            prod.ArchivedAt = null;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Product",
                    EntityId = productId.ToString(),
                    Operation = "Restore",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { ProductId = productId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // CATEGORIES
        // =========================================================================

        public async Task<Category> AddCategoryAsync(int companyId, Category category, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            category.CompanyId = companyId;
            category.Id = 0;
            context.Categories.Add(category);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Category",
                    EntityId = category.Id.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(category),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return category;
        }

        public async Task<Category?> UpdateCategoryAsync(int companyId, int categoryId, string newName, string? description = null, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.Name = newName;
            if (description != null) existing.Description = description;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Category",
                    EntityId = existing.Id.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(existing),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<Category?> UpdateCategoryAsync(int companyId, Category category, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id).ConfigureAwait(false);
            if (existing == null) return null;

            existing.Name = category.Name;
            existing.Icon = category.Icon;
            existing.Description = category.Description;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Category",
                    EntityId = category.Id.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(category),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> DeleteCategoryAsync(int companyId, int categoryId, string? fallback = null, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId).ConfigureAwait(false);
            if (existing == null) return false;

            if (!string.IsNullOrWhiteSpace(fallback))
            {
                var prods = await context.Products.Where(p => p.CategoryName == existing.Name).ToListAsync().ConfigureAwait(false);
                foreach (var p in prods)
                {
                    p.CategoryName = fallback;
                }
            }

            context.Categories.Remove(existing);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Category",
                    EntityId = categoryId.ToString(),
                    Operation = "Delete",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { CategoryId = categoryId, Fallback = fallback }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // CUSTOMERS
        // =========================================================================

        public async Task<Customer> AddCustomerAsync(int companyId, Customer customer, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            customer.CustomerId = 0;
            context.Customers.Add(customer);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Customer",
                    EntityId = customer.CustomerId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(customer),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return customer;
        }

        public async Task<Customer?> UpdateCustomerAsync(int companyId, Customer customer, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.CustomerCode = customer.CustomerCode;
            existing.CustomerName = customer.CustomerName;
            existing.ContactNumber = customer.ContactNumber;
            existing.EmailAddress = customer.EmailAddress;
            existing.Address = customer.Address;
            existing.IsActive = customer.IsActive;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Customer",
                    EntityId = customer.CustomerId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(customer),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> DeleteCustomerAsync(int companyId, int customerId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.IsActive = false;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Customer",
                    EntityId = customerId.ToString(),
                    Operation = "Delete",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { CustomerId = customerId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // SUPPLIERS
        // =========================================================================

        public async Task<Supplier> AddSupplierAsync(int companyId, Supplier supplier, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            supplier.SupplierId = 0;
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Supplier",
                    EntityId = supplier.SupplierId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(supplier),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return supplier;
        }

        public async Task<Supplier?> UpdateSupplierAsync(int companyId, Supplier supplier, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplier.SupplierId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.SupplierCode = supplier.SupplierCode;
            existing.SupplierName = supplier.SupplierName;
            existing.ContactPerson = supplier.ContactPerson;
            existing.ContactNumber = supplier.ContactNumber;
            existing.EmailAddress = supplier.EmailAddress;
            existing.Address = supplier.Address;
            existing.IsActive = supplier.IsActive;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Supplier",
                    EntityId = supplier.SupplierId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(supplier),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> DeleteSupplierAsync(int companyId, int supplierId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.IsActive = false;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Supplier",
                    EntityId = supplierId.ToString(),
                    Operation = "Delete",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { SupplierId = supplierId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // REPAIR TICKETS
        // =========================================================================

        public async Task<RepairTicket> AddRepairTicketAsync(int companyId, RepairTicket ticket, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            ticket.CompanyId = companyId;
            ticket.RepairTicketId = 0;
            if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                ticket.TicketNumber = $"REP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.IsActive = true;

            context.RepairTickets.Add(ticket);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "RepairTicket",
                    EntityId = ticket.TicketNumber,
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(ticket),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return ticket;
        }

        public async Task<bool> UpdateRepairStatusAsync(int companyId, int ticketId, string status, string? notes, string? technician, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.RepairTickets.FirstOrDefaultAsync(t => t.RepairTicketId == ticketId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.Status = status;
            if (!string.IsNullOrEmpty(notes)) existing.DiagnosticNotes = notes;
            if (!string.IsNullOrEmpty(technician)) existing.AssignedTechnician = technician;
            if (status == "Completed") existing.CompletedAt = DateTime.UtcNow;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "RepairTicket",
                    EntityId = ticketId.ToString(),
                    Operation = "UpdateStatus",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Status = status, DiagnosticNotes = notes, AssignedTechnician = technician }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        public async Task<bool> UpdateRepairBillingAsync(int companyId, int ticketId, decimal laborFee, decimal partsCost, decimal depositAmount, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.RepairTickets.FirstOrDefaultAsync(t => t.RepairTicketId == ticketId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.LaborFee = laborFee;
            existing.PartsCost = partsCost;
            existing.DepositAmount = depositAmount;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "RepairTicket",
                    EntityId = ticketId.ToString(),
                    Operation = "UpdateBilling",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { LaborFee = laborFee, PartsCost = partsCost, DepositAmount = depositAmount }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // STAFF MEMBERS
        // =========================================================================

        public async Task<StaffMember> AddStaffMemberAsync(int companyId, StaffMember staff, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            staff.CompanyId = companyId;
            staff.StaffId = 0;
            context.StaffMembers.Add(staff);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StaffMember",
                    EntityId = staff.StaffId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(staff),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return staff;
        }

        public async Task<StaffMember?> UpdateStaffMemberAsync(int companyId, StaffMember staff, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.StaffMembers.FirstOrDefaultAsync(s => s.StaffId == staff.StaffId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.StaffCode = staff.StaffCode;
            existing.FullName = staff.FullName;
            existing.Username = staff.Username;
            existing.Role = staff.Role;
            existing.PositionTitle = staff.PositionTitle;
            existing.Email = staff.Email;
            existing.PhoneNumber = staff.PhoneNumber;
            existing.HourlyRate = staff.HourlyRate;
            existing.MonthlySalary = staff.MonthlySalary;
            existing.IsActive = staff.IsActive;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StaffMember",
                    EntityId = staff.StaffId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(staff),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> DeleteStaffMemberAsync(int companyId, int staffId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.StaffMembers.FirstOrDefaultAsync(s => s.StaffId == staffId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.IsActive = false;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StaffMember",
                    EntityId = staffId.ToString(),
                    Operation = "Delete",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { StaffId = staffId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // PAYROLL RECORDS
        // =========================================================================

        public async Task<PayrollRecord> AddPayrollRecordAsync(int companyId, PayrollRecord payroll, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            payroll.CompanyId = companyId;
            payroll.PayrollId = 0;
            context.PayrollRecords.Add(payroll);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "PayrollRecord",
                    EntityId = payroll.PayrollId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(payroll),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return payroll;
        }

        public async Task<bool> DeletePayrollRecordAsync(int companyId, int payrollId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var record = await context.PayrollRecords.FirstOrDefaultAsync(p => p.PayrollId == payrollId && p.CompanyId == companyId).ConfigureAwait(false);
            if (record == null) return false;

            context.PayrollRecords.Remove(record);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "PayrollRecord",
                    EntityId = payrollId.ToString(),
                    Operation = "Delete",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { PayrollId = payrollId, CompanyId = companyId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return true;
        }

        // =========================================================================
        // APPROVAL REQUESTS
        // =========================================================================

        public async Task<ApprovalRequest> CreateApprovalRequestAsync(int companyId, ApprovalRequest request, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            request.CompanyId = companyId;
            request.RequestId = 0;
            if (string.IsNullOrWhiteSpace(request.RequestNumber))
            {
                request.RequestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "Pending";

            context.ApprovalRequests.Add(request);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "ApprovalRequest",
                    EntityId = request.RequestNumber,
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(request),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return request;
        }

        public async Task<bool> ResolveApprovalRequestAsync(int companyId, int requestId, string status, string reviewer, string? notes, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.ApprovalRequests.FirstOrDefaultAsync(r => r.RequestId == requestId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.Status = status;
            existing.ReviewedBy = reviewer;
            existing.ReviewNotes = notes;
            existing.ResolvedAt = DateTime.UtcNow;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "ApprovalRequest",
                    EntityId = requestId.ToString(),
                    Operation = "Resolve",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Status = status, ReviewedBy = reviewer, ReviewNotes = notes }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // STORE POLICIES
        // =========================================================================

        public async Task<StorePolicy> AddStorePolicyAsync(int companyId, StorePolicy policy, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            policy.CompanyId = companyId;
            policy.PolicyId = 0;
            context.StorePolicies.Add(policy);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StorePolicy",
                    EntityId = policy.PolicyId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(policy),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return policy;
        }

        public async Task<StorePolicy?> UpdateStorePolicyAsync(int companyId, string policyType, string content, string updatedBy, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.StorePolicies.FirstOrDefaultAsync(p => p.PolicyType == policyType).ConfigureAwait(false);
            if (existing == null) return null;

            existing.ContentText = content;
            existing.LastUpdatedBy = updatedBy;
            existing.UpdatedAt = DateTime.UtcNow;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StorePolicy",
                    EntityId = existing.PolicyId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(existing),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<StorePolicy?> UpdateStorePolicyAsync(int companyId, StorePolicy policy, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.StorePolicies.FirstOrDefaultAsync(p => p.PolicyId == policy.PolicyId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.Title = policy.Title;
            existing.ContentText = policy.ContentText;
            existing.PolicyType = policy.PolicyType;
            existing.LastUpdatedBy = policy.LastUpdatedBy;
            existing.UpdatedAt = DateTime.UtcNow;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StorePolicy",
                    EntityId = policy.PolicyId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(policy),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> DeleteStorePolicyAsync(int companyId, int policyId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.StorePolicies.FirstOrDefaultAsync(p => p.PolicyId == policyId).ConfigureAwait(false);
            if (existing == null) return false;

            context.StorePolicies.Remove(existing);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "StorePolicy",
                    EntityId = policyId.ToString(),
                    Operation = "Delete",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { PolicyId = policyId }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // EXPENSE OPERATIONS
        // =========================================================================
        public async Task<ExpenseRecord?> SaveExpenseAsync(int companyId, ExpenseRecord expense, bool enqueueSync = true)
        {
            if (expense == null) return null;
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);

            expense.CompanyId = companyId;
            expense.ExpenseId = 0;
            if (string.IsNullOrWhiteSpace(expense.ExpenseNumber))
            {
                expense.ExpenseNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            if (expense.ExpenseDate == default)
            {
                expense.ExpenseDate = DateTime.UtcNow;
            }
            expense.CreatedAt = DateTime.UtcNow;

            context.Expenses.Add(expense);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Expense",
                    EntityId = expense.ExpenseId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(expense),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return expense;
        }

        public async Task<ExpenseRecord?> UpdateExpenseAsync(int companyId, ExpenseRecord expense, bool enqueueSync = true)
        {
            if (expense == null) return null;
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);

            var existing = await context.Expenses.FirstOrDefaultAsync(e => e.ExpenseId == expense.ExpenseId && e.CompanyId == companyId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.Category = expense.Category;
            existing.Description = expense.Description;
            existing.Amount = expense.Amount;
            existing.PaidTo = expense.PaidTo;
            existing.PaymentMethod = expense.PaymentMethod;
            existing.RecordedBy = expense.RecordedBy;
            existing.ReceiptRef = expense.ReceiptRef;
            existing.Notes = expense.Notes;
            existing.IsTaxDeductible = expense.IsTaxDeductible;
            existing.IsActive = expense.IsActive;
            if (expense.ExpenseDate != default)
            {
                existing.ExpenseDate = expense.ExpenseDate;
            }

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Expense",
                    EntityId = existing.ExpenseId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(existing),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> ToggleExpenseArchiveAsync(int companyId, int expenseId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Expenses.FirstOrDefaultAsync(e => e.ExpenseId == expenseId && e.CompanyId == companyId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.IsActive = !existing.IsActive;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Expense",
                    EntityId = expenseId.ToString(),
                    Operation = "Archive",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { ExpenseId = expenseId, IsActive = existing.IsActive }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // BRANCH OPERATIONS (Medium Enterprise)
        // =========================================================================
        public async Task<Branch?> SaveBranchAsync(int companyId, Branch branch, bool enqueueSync = true)
        {
            if (branch == null) return null;
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);

            branch.CompanyId = companyId;
            branch.BranchId = 0;
            if (string.IsNullOrWhiteSpace(branch.BranchCode))
            {
                int count = await context.Branches.CountAsync(b => b.CompanyId == companyId).ConfigureAwait(false);
                branch.BranchCode = $"BR-{(count + 1):D3}";
            }
            branch.CreatedAt = DateTime.UtcNow;

            context.Branches.Add(branch);
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Branch",
                    EntityId = branch.BranchId.ToString(),
                    Operation = "Create",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(branch),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return branch;
        }

        public async Task<Branch?> UpdateBranchAsync(int companyId, Branch branch, bool enqueueSync = true)
        {
            if (branch == null) return null;
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);

            var existing = await context.Branches.FirstOrDefaultAsync(b => b.BranchId == branch.BranchId && b.CompanyId == companyId).ConfigureAwait(false);
            if (existing == null) return null;

            existing.BranchCode = branch.BranchCode;
            existing.BranchName = branch.BranchName;
            existing.Address = branch.Address;
            existing.City = branch.City;
            existing.ContactNumber = branch.ContactNumber;
            existing.ManagerName = branch.ManagerName;
            existing.AssignedStaffCount = branch.AssignedStaffCount;
            existing.IsActive = branch.IsActive;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Branch",
                    EntityId = existing.BranchId.ToString(),
                    Operation = "Update",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(existing),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return existing;
        }

        public async Task<bool> ToggleBranchArchiveAsync(int companyId, int branchId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var existing = await context.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId && b.CompanyId == companyId).ConfigureAwait(false);
            if (existing == null) return false;

            existing.IsActive = !existing.IsActive;

            if (enqueueSync)
            {
                context.SyncOutbox.Add(new SyncOutboxItem
                {
                    SyncId = Guid.NewGuid().ToString("N"),
                    CompanyId = companyId,
                    EntityType = "Branch",
                    EntityId = branchId.ToString(),
                    Operation = "Archive",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { BranchId = branchId, IsActive = existing.IsActive }),
                    CreatedAt = DateTime.UtcNow,
                    SyncStatus = "Pending"
                });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        // =========================================================================
        // PROCUREMENT / PURCHASE ORDER OPERATIONS (Medium Enterprise)
        // =========================================================================
        public async Task<PurchaseOrder?> SavePurchaseOrderAsync(int companyId, PurchaseOrder order, bool enqueueSync = true)
        {
            if (order == null) return null;
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                order.CompanyId = companyId;
                order.PurchaseOrderId = 0;
                if (string.IsNullOrWhiteSpace(order.PurchaseOrderNumber))
                {
                    int count = await context.PurchaseOrders.CountAsync(p => p.CompanyId == companyId).ConfigureAwait(false);
                    order.PurchaseOrderNumber = $"PO-{DateTime.UtcNow:yyyy}-{(count + 1):D3}";
                }
                order.CreatedAt = DateTime.UtcNow;
                if (order.OrderDate == default) order.OrderDate = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(order.Status)) order.Status = "In Transit";

                foreach (var item in order.Items)
                {
                    item.PurchaseOrderId = 0;
                    item.PurchaseOrderItemId = 0;
                }

                context.PurchaseOrders.Add(order);
                await context.SaveChangesAsync().ConfigureAwait(false);

                if (enqueueSync)
                {
                    context.SyncOutbox.Add(new SyncOutboxItem
                    {
                        SyncId = Guid.NewGuid().ToString("N"),
                        CompanyId = companyId,
                        EntityType = "PurchaseOrder",
                        EntityId = order.PurchaseOrderId.ToString(),
                        Operation = "Create",
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(order),
                        CreatedAt = DateTime.UtcNow,
                        SyncStatus = "Pending"
                    });
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }

                await tx.CommitAsync().ConfigureAwait(false);
                return order;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"SavePurchaseOrderAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<PurchaseOrder?> UpdatePurchaseOrderAsync(int companyId, PurchaseOrder order, bool enqueueSync = true)
        {
            if (order == null || order.PurchaseOrderId <= 0) return null;
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                var existing = await context.PurchaseOrders
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == order.PurchaseOrderId && p.CompanyId == companyId)
                    .ConfigureAwait(false);

                if (existing == null) return null;

                existing.PurchaseOrderNumber = order.PurchaseOrderNumber;
                existing.SupplierId = order.SupplierId;
                existing.SupplierName = order.SupplierName;
                existing.OrderDate = order.OrderDate;
                existing.ExpectedDeliveryDate = order.ExpectedDeliveryDate;
                existing.Status = order.Status;
                existing.TotalAmount = order.TotalAmount;
                existing.Notes = order.Notes;
                existing.ApprovedBy = order.ApprovedBy;
                existing.ReceivedDate = order.ReceivedDate;
                existing.IsActive = order.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                if (order.Items != null && order.Items.Count > 0)
                {
                    context.PurchaseOrderItems.RemoveRange(existing.Items);
                    existing.Items.Clear();

                    foreach (var item in order.Items)
                    {
                        item.PurchaseOrderId = existing.PurchaseOrderId;
                        item.PurchaseOrderItemId = 0;
                        existing.Items.Add(item);
                    }
                }

                if (enqueueSync)
                {
                    context.SyncOutbox.Add(new SyncOutboxItem
                    {
                        SyncId = Guid.NewGuid().ToString("N"),
                        CompanyId = companyId,
                        EntityType = "PurchaseOrder",
                        EntityId = existing.PurchaseOrderId.ToString(),
                        Operation = "Update",
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(existing),
                        CreatedAt = DateTime.UtcNow,
                        SyncStatus = "Pending"
                    });
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return existing;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"UpdatePurchaseOrderAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> TogglePurchaseOrderArchiveAsync(int companyId, int purchaseOrderId, bool enqueueSync = true)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                var existing = await context.PurchaseOrders
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId && p.CompanyId == companyId)
                    .ConfigureAwait(false);

                if (existing == null) return false;

                existing.IsActive = !existing.IsActive;
                if (!existing.IsActive)
                {
                    existing.Status = "Cancelled";
                }
                existing.UpdatedAt = DateTime.UtcNow;

                if (enqueueSync)
                {
                    context.SyncOutbox.Add(new SyncOutboxItem
                    {
                        SyncId = Guid.NewGuid().ToString("N"),
                        CompanyId = companyId,
                        EntityType = "PurchaseOrder",
                        EntityId = purchaseOrderId.ToString(),
                        Operation = "Archive",
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { PurchaseOrderId = purchaseOrderId, IsActive = existing.IsActive, Status = existing.Status }),
                        CreatedAt = DateTime.UtcNow,
                        SyncStatus = "Pending"
                    });
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"TogglePurchaseOrderArchiveAsync error: {ex.Message}");
                throw;
            }
        }

        // =========================================================================
        // SYNC OUTBOX REPOSITORY METHODS
        // =========================================================================

        /// <summary>
        /// Retrieves pending and retryable failed outbox items for the specified tenant, ordered by creation time.
        /// </summary>
        public async Task<List<SyncOutboxItem>> GetPendingOutboxItemsAsync(int companyId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            return await context.SyncOutbox
                .Where(o => o.CompanyId == companyId && (o.SyncStatus == "Pending" || o.SyncStatus == "Failed") && o.RetryCount < 10)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Marks an outbox item as Synced.
        /// </summary>
        public async Task MarkOutboxSyncedAsync(int companyId, string syncId)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var item = await context.SyncOutbox.FirstOrDefaultAsync(o => o.SyncId == syncId).ConfigureAwait(false);
            if (item != null)
            {
                item.SyncStatus = "Synced";
                item.LastAttemptAt = DateTime.UtcNow;
                item.ErrorMessage = null;
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Increments retry count and marks an outbox item as Failed with error message.
        /// </summary>
        public async Task MarkOutboxFailedAsync(int companyId, string syncId, string errorMessage)
        {
            await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);
            var item = await context.SyncOutbox.FirstOrDefaultAsync(o => o.SyncId == syncId).ConfigureAwait(false);
            if (item != null)
            {
                item.SyncStatus = "Failed";
                item.RetryCount++;
                item.LastAttemptAt = DateTime.UtcNow;
                item.ErrorMessage = errorMessage.Length > 2000 ? errorMessage.Substring(0, 1997) + "..." : errorMessage;
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        // =========================================================================
        // CONTINUOUS CLOUD -> LOCAL DATABASE REFRESH
        // =========================================================================

        /// <summary>
        /// Performs continuous online Cloud -> Local database refresh for the specified tenant.
        /// Resolves the local database dynamically from ERP_Master_Local.CompanyDatabases.
        /// Strict dependency order: Categories -> Products -> Inventories -> Customers -> Suppliers -> StaffMembers -> Orders -> RepairTickets -> PayrollRecords -> ApprovalRequests -> StorePolicies.
        /// Strictly protects any local record that has an active (Pending, Processing, Failed) SyncOutbox mutation.
        /// Preserves cloud primary keys using IDENTITY_INSERT ON when inserting missing records.
        /// Deletes local records removed from cloud ONLY when they do not have active SyncOutbox mutations.
        /// Does NOT generate new outbound SyncOutbox records (inbound refresh only).
        /// </summary>
        public async Task<CloudSyncResult> RefreshFromCloudAsync(int companyId, ApiClient? apiClient = null)
        {
            apiClient ??= ApiClient.Instance;
            var result = new CloudSyncResult();

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                result.Success = false;
                result.Message = "Network interface offline.";
                return result;
            }

            bool isReachable = await apiClient.CheckApiConnectivityAsync().ConfigureAwait(false);
            if (!isReachable)
            {
                result.Success = false;
                result.Message = "ERP.api is unreachable.";
                return result;
            }

            try
            {
                await using var context = await LocalTenantDbContextProvider.CreateTenantDbContextAsync(companyId).ConfigureAwait(false);

                var activeOutbox = await context.SyncOutbox
                    .AsNoTracking()
                    .Where(o => o.CompanyId == companyId && (o.SyncStatus == "Pending" || o.SyncStatus == "Processing" || o.SyncStatus == "Failed"))
                    .ToListAsync()
                    .ConfigureAwait(false);

                bool HasOutbox(string entityType, params string?[] candidateIds)
                {
                    return activeOutbox.Any(o =>
                        string.Equals(o.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
                        candidateIds.Any(id => !string.IsNullOrEmpty(id) && string.Equals(o.EntityId, id, StringComparison.OrdinalIgnoreCase)));
                }

                bool HasActiveOrderForProduct(int productId)
                {
                    foreach (var o in activeOutbox.Where(x => string.Equals(x.EntityType, "Order", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (o.PayloadJson.Contains($"\"ProductId\":{productId}") ||
                            o.PayloadJson.Contains($"\"productId\":{productId}") ||
                            o.PayloadJson.Contains($"\"ProductId\": {productId}") ||
                            o.PayloadJson.Contains($"\"productId\": {productId}"))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                // 1. Categories
                var cloudCats = await apiClient.GetCategoriesAsync(companyId).ConfigureAwait(false);
                if (cloudCats != null)
                {
                    var localCats = await context.Categories.ToListAsync().ConfigureAwait(false);
                    var newCats = new List<Category>();

                    foreach (var cloud in cloudCats)
                    {
                        var local = localCats.FirstOrDefault(c => c.Id == cloud.Id || c.Name.Equals(cloud.Name, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("Category", local.Id.ToString(), local.Name, cloud.Id.ToString(), cloud.Name))
                            {
                                local.Name = cloud.Name;
                                local.Description = cloud.Description;
                                if (!string.IsNullOrEmpty(cloud.Icon)) local.Icon = cloud.Icon;
                            }
                        }
                        else
                        {
                            newCats.Add(new Category
                            {
                                Id = cloud.Id,
                                Name = cloud.Name,
                                Description = cloud.Description,
                                Icon = cloud.Icon
                            });
                        }
                    }

                    foreach (var local in localCats)
                    {
                        if (cloudCats.All(c => c.Id != local.Id && !c.Name.Equals(local.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("Category", local.Id.ToString(), local.Name))
                            {
                                bool isReferenced = await context.Products.AnyAsync(p => p.CategoryName == local.Name).ConfigureAwait(false);
                                if (!isReferenced)
                                {
                                    context.Categories.Remove(local);
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newCats.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "Categories", newCats).ConfigureAwait(false);
                    }
                    result.CategoriesCount = cloudCats.Count;
                }

                // 2. Products
                var cloudProds = await apiClient.GetProductsAsync(companyId).ConfigureAwait(false);
                if (cloudProds != null)
                {
                    var localProds = await context.Products.ToListAsync().ConfigureAwait(false);
                    var newProds = new List<Product>();

                    foreach (var cloud in cloudProds)
                    {
                        var local = localProds.FirstOrDefault(p => (cloud.ProductId > 0 && p.ProductId == cloud.ProductId) || p.ProductCode.Equals(cloud.ProductCode, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("Product", local.ProductId.ToString(), local.ProductCode, cloud.ProductId.ToString(), cloud.ProductCode))
                            {
                                local.ProductName = cloud.ProductName;
                                local.ProductCode = cloud.ProductCode;
                                local.UnitPrice = cloud.UnitPrice;
                                local.CategoryName = cloud.CategoryName;
                                local.Description = cloud.Description;
                                local.IsActive = cloud.IsActive;
                                local.CreatedAt = cloud.CreatedAt;
                                local.ArchivedAt = cloud.ArchivedAt;
                                if (!string.IsNullOrEmpty(cloud.SupplierName)) local.SupplierName = cloud.SupplierName;
                                if (cloud.SupplierId.HasValue) local.SupplierId = cloud.SupplierId;
                            }
                        }
                        else
                        {
                            newProds.Add(new Product
                            {
                                ProductId = cloud.ProductId,
                                ProductCode = cloud.ProductCode,
                                ProductName = cloud.ProductName,
                                UnitPrice = cloud.UnitPrice,
                                CategoryName = cloud.CategoryName,
                                Description = cloud.Description,
                                IsActive = cloud.IsActive,
                                CreatedAt = cloud.CreatedAt,
                                ArchivedAt = cloud.ArchivedAt,
                                SupplierName = cloud.SupplierName,
                                SupplierId = cloud.SupplierId
                            });
                        }
                    }

                    foreach (var local in localProds)
                    {
                        if (cloudProds.All(p => p.ProductId != local.ProductId && !p.ProductCode.Equals(local.ProductCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("Product", local.ProductId.ToString(), local.ProductCode))
                            {
                                try
                                {
                                    context.Products.Remove(local);
                                }
                                catch
                                {
                                    local.IsActive = false;
                                    local.ArchivedAt = DateTime.UtcNow;
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newProds.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "Products", newProds).ConfigureAwait(false);
                    }
                    result.ProductsCount = cloudProds.Count;
                }

                // 3. Inventories
                var cloudInvs = await apiClient.GetInventoriesAsync(companyId).ConfigureAwait(false);
                var localInvs = await context.Inventories.ToListAsync().ConfigureAwait(false);
                var newInvs = new List<Inventory>();

                if (cloudInvs != null && cloudInvs.Count > 0)
                {
                    foreach (var cloud in cloudInvs)
                    {
                        if (HasOutbox("Product", cloud.ProductId.ToString()) || HasActiveOrderForProduct(cloud.ProductId))
                        {
                            continue;
                        }

                        var local = localInvs.FirstOrDefault(i => i.ProductId == cloud.ProductId || (cloud.InventoryId > 0 && i.InventoryId == cloud.InventoryId));
                        if (local != null)
                        {
                            local.QuantityOnHand = cloud.QuantityOnHand;
                            local.ReorderLevel = cloud.ReorderLevel;
                            local.LastUpdatedAt = cloud.LastUpdatedAt;
                        }
                        else
                        {
                            bool productExistsLocally = await context.Products.AnyAsync(p => p.ProductId == cloud.ProductId).ConfigureAwait(false);
                            if (productExistsLocally)
                            {
                                newInvs.Add(new Inventory
                                {
                                    InventoryId = cloud.InventoryId,
                                    ProductId = cloud.ProductId,
                                    QuantityOnHand = cloud.QuantityOnHand,
                                    ReorderLevel = cloud.ReorderLevel,
                                    LastUpdatedAt = cloud.LastUpdatedAt
                                });
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newInvs.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "Inventories", newInvs).ConfigureAwait(false);
                    }
                    result.InventoriesCount = cloudInvs.Count;
                }
                else if (cloudProds != null)
                {
                    foreach (var p in cloudProds)
                    {
                        if (HasOutbox("Product", p.ProductId.ToString(), p.ProductCode) || HasActiveOrderForProduct(p.ProductId))
                        {
                            continue;
                        }

                        var local = localInvs.FirstOrDefault(i => i.ProductId == p.ProductId);
                        if (local != null)
                        {
                            local.QuantityOnHand = p.StockQuantity;
                            local.LastUpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            bool productExistsLocally = await context.Products.AnyAsync(pr => pr.ProductId == p.ProductId).ConfigureAwait(false);
                            if (productExistsLocally)
                            {
                                context.Inventories.Add(new Inventory
                                {
                                    ProductId = p.ProductId,
                                    QuantityOnHand = p.StockQuantity,
                                    ReorderLevel = 3,
                                    LastUpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    result.InventoriesCount = cloudProds.Count;
                }

                // 4. Customers
                var cloudCusts = await apiClient.GetCustomersAsync(companyId).ConfigureAwait(false);
                if (cloudCusts != null)
                {
                    var localCusts = await context.Customers.ToListAsync().ConfigureAwait(false);
                    var newCusts = new List<Customer>();

                    foreach (var cloud in cloudCusts)
                    {
                        var local = localCusts.FirstOrDefault(c => (cloud.CustomerId > 0 && c.CustomerId == cloud.CustomerId) || c.CustomerCode.Equals(cloud.CustomerCode, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("Customer", local.CustomerId.ToString(), local.CustomerCode, cloud.CustomerId.ToString(), cloud.CustomerCode))
                            {
                                local.CustomerCode = cloud.CustomerCode;
                                local.CustomerCode = cloud.CustomerCode;
                                local.CustomerName = cloud.CustomerName;
                                local.EmailAddress = cloud.EmailAddress;
                                local.ContactNumber = cloud.ContactNumber;
                                local.Address = cloud.Address;
                                local.CreatedAt = cloud.CreatedAt;
                                local.IsActive = cloud.IsActive;
                            }
                        }
                        else
                        {
                            newCusts.Add(new Customer
                            {
                                CustomerId = cloud.CustomerId,
                                CustomerCode = cloud.CustomerCode,
                                CustomerName = cloud.CustomerName,
                                EmailAddress = cloud.EmailAddress,
                                ContactNumber = cloud.ContactNumber,
                                Address = cloud.Address,
                                CreatedAt = cloud.CreatedAt,
                                IsActive = cloud.IsActive
                            });
                        }
                    }

                    foreach (var local in localCusts)
                    {
                        if (cloudCusts.All(c => c.CustomerId != local.CustomerId && !c.CustomerCode.Equals(local.CustomerCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("Customer", local.CustomerId.ToString(), local.CustomerCode))
                            {
                                context.Customers.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newCusts.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "Customers", newCusts).ConfigureAwait(false);
                    }
                    result.CustomersCount = cloudCusts.Count;
                }

                // 5. Suppliers
                var cloudSups = await apiClient.GetSuppliersAsync(companyId).ConfigureAwait(false);
                if (cloudSups != null)
                {
                    var localSups = await context.Suppliers.ToListAsync().ConfigureAwait(false);
                    var newSups = new List<Supplier>();

                    foreach (var cloud in cloudSups)
                    {
                        var local = localSups.FirstOrDefault(s => (cloud.SupplierId > 0 && s.SupplierId == cloud.SupplierId) || s.SupplierCode.Equals(cloud.SupplierCode, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("Supplier", local.SupplierId.ToString(), local.SupplierCode, cloud.SupplierId.ToString(), cloud.SupplierCode))
                            {
                                local.SupplierCode = cloud.SupplierCode;
                                local.SupplierName = cloud.SupplierName;
                                local.ContactPerson = cloud.ContactPerson;
                                local.EmailAddress = cloud.EmailAddress;
                                local.ContactNumber = cloud.ContactNumber;
                                local.Address = cloud.Address;
                                local.IsActive = cloud.IsActive;
                                local.CreatedAt = cloud.CreatedAt;
                            }
                        }
                        else
                        {
                            newSups.Add(new Supplier
                            {
                                SupplierId = cloud.SupplierId,
                                SupplierCode = cloud.SupplierCode,
                                SupplierName = cloud.SupplierName,
                                ContactPerson = cloud.ContactPerson,
                                EmailAddress = cloud.EmailAddress,
                                ContactNumber = cloud.ContactNumber,
                                Address = cloud.Address,
                                IsActive = cloud.IsActive,
                                CreatedAt = cloud.CreatedAt
                            });
                        }
                    }

                    foreach (var local in localSups)
                    {
                        if (cloudSups.All(s => s.SupplierId != local.SupplierId && !s.SupplierCode.Equals(local.SupplierCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("Supplier", local.SupplierId.ToString(), local.SupplierCode))
                            {
                                context.Suppliers.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newSups.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "Suppliers", newSups).ConfigureAwait(false);
                    }
                    result.SuppliersCount = cloudSups.Count;
                }

                // 6. StaffMembers
                var cloudStaff = await apiClient.GetStaffAsync(companyId).ConfigureAwait(false);
                if (cloudStaff != null)
                {
                    var localStaff = await context.StaffMembers.ToListAsync().ConfigureAwait(false);
                    var newStaff = new List<StaffMember>();

                    foreach (var cloud in cloudStaff)
                    {
                        var local = localStaff.FirstOrDefault(s => (cloud.StaffId > 0 && s.StaffId == cloud.StaffId) || s.StaffCode.Equals(cloud.StaffCode, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("StaffMember", local.StaffId.ToString(), local.StaffCode, cloud.StaffId.ToString(), cloud.StaffCode))
                            {
                                local.StaffCode = cloud.StaffCode;
                                local.FullName = cloud.FullName;
                                local.Username = cloud.Username;
                                local.Role = cloud.Role;
                                local.PositionTitle = cloud.PositionTitle;
                                local.HourlyRate = cloud.HourlyRate;
                                local.MonthlySalary = cloud.MonthlySalary;
                                local.IsActive = cloud.IsActive;
                                local.HiredDate = cloud.HiredDate;
                            }
                        }
                        else
                        {
                            newStaff.Add(new StaffMember
                            {
                                StaffId = cloud.StaffId,
                                StaffCode = cloud.StaffCode,
                                FullName = cloud.FullName,
                                Username = cloud.Username,
                                Role = cloud.Role,
                                PositionTitle = cloud.PositionTitle,
                                HourlyRate = cloud.HourlyRate,
                                MonthlySalary = cloud.MonthlySalary,
                                IsActive = cloud.IsActive,
                                HiredDate = cloud.HiredDate
                            });
                        }
                    }

                    foreach (var local in localStaff)
                    {
                        if (cloudStaff.All(s => s.StaffId != local.StaffId && !s.StaffCode.Equals(local.StaffCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("StaffMember", local.StaffId.ToString(), local.StaffCode))
                            {
                                context.StaffMembers.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newStaff.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "StaffMembers", newStaff).ConfigureAwait(false);
                    }
                    result.StaffCount = cloudStaff.Count;
                }

                // 7. Orders
                var cloudOrders = await apiClient.GetOrdersAsync(companyId).ConfigureAwait(false);
                if (cloudOrders != null)
                {
                    var localOrders = await context.Orders.ToListAsync().ConfigureAwait(false);
                    var newOrders = new List<Order>();

                    foreach (var cloud in cloudOrders)
                    {
                        var local = localOrders.FirstOrDefault(o => o.Id == cloud.Id);
                        if (local != null)
                        {
                            if (!HasOutbox("Order", local.Id, cloud.Id))
                            {
                                local.CompanyId = cloud.CompanyId;
                                local.CustomerName = cloud.CustomerName;
                                local.CreatedAt = cloud.CreatedAt;
                                local.ItemsJson = cloud.ItemsJson;
                                local.Subtotal = cloud.Subtotal;
                                local.Discount = cloud.Discount;
                                local.Tax = cloud.Tax;
                                local.TotalAmount = cloud.TotalAmount;
                                local.PaymentMethod = cloud.PaymentMethod;
                                local.CashierName = cloud.CashierName;
                            }
                        }
                        else
                        {
                            newOrders.Add(new Order
                            {
                                Id = cloud.Id,
                                CompanyId = cloud.CompanyId,
                                CustomerName = cloud.CustomerName,
                                CreatedAt = cloud.CreatedAt,
                                ItemsJson = cloud.ItemsJson,
                                Subtotal = cloud.Subtotal,
                                Discount = cloud.Discount,
                                Tax = cloud.Tax,
                                TotalAmount = cloud.TotalAmount,
                                PaymentMethod = cloud.PaymentMethod,
                                CashierName = cloud.CashierName
                            });
                        }
                    }

                    foreach (var local in localOrders)
                    {
                        if (cloudOrders.All(o => o.Id != local.Id))
                        {
                            if (!HasOutbox("Order", local.Id))
                            {
                                context.Orders.Remove(local);
                            }
                        }
                    }

                    if (newOrders.Count > 0)
                    {
                        context.Orders.AddRange(newOrders);
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                    result.OrdersCount = cloudOrders.Count;
                }

                // 8. RepairTickets
                var cloudRepairs = await apiClient.GetRepairsAsync(companyId).ConfigureAwait(false);
                if (cloudRepairs != null)
                {
                    var localRepairs = await context.RepairTickets.ToListAsync().ConfigureAwait(false);
                    var newRepairs = new List<RepairTicket>();

                    foreach (var cloud in cloudRepairs)
                    {
                        var local = localRepairs.FirstOrDefault(r => (cloud.RepairTicketId > 0 && r.RepairTicketId == cloud.RepairTicketId) || r.TicketNumber.Equals(cloud.TicketNumber, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("RepairTicket", local.RepairTicketId.ToString(), local.TicketNumber, cloud.RepairTicketId.ToString(), cloud.TicketNumber))
                            {
                                local.TicketNumber = cloud.TicketNumber;
                                local.CustomerName = cloud.CustomerName;
                                local.CustomerPhone = cloud.CustomerPhone;
                                local.CustomerEmail = cloud.CustomerEmail;
                                local.DeviceType = cloud.DeviceType;
                                local.DeviceBrandModel = cloud.DeviceBrandModel;
                                local.SerialNumber = cloud.SerialNumber;
                                local.ReportedIssue = cloud.ReportedIssue;
                                local.DiagnosticNotes = cloud.DiagnosticNotes;
                                local.AssignedTechnician = cloud.AssignedTechnician;
                                local.Status = cloud.Status;
                                local.LaborFee = cloud.LaborFee;
                                local.PartsCost = cloud.PartsCost;
                                local.DepositAmount = cloud.DepositAmount;
                                local.WarrantyTerms = cloud.WarrantyTerms;
                                local.CreatedAt = cloud.CreatedAt;
                                local.CompletedAt = cloud.CompletedAt;
                                if (!string.IsNullOrEmpty(cloud.PartsSupplier)) local.PartsSupplier = cloud.PartsSupplier;
                            }
                        }
                        else
                        {
                            newRepairs.Add(new RepairTicket
                            {
                                RepairTicketId = cloud.RepairTicketId,
                                TicketNumber = cloud.TicketNumber,
                                CustomerName = cloud.CustomerName,
                                CustomerPhone = cloud.CustomerPhone,
                                CustomerEmail = cloud.CustomerEmail,
                                DeviceType = cloud.DeviceType,
                                DeviceBrandModel = cloud.DeviceBrandModel,
                                SerialNumber = cloud.SerialNumber,
                                ReportedIssue = cloud.ReportedIssue,
                                DiagnosticNotes = cloud.DiagnosticNotes,
                                AssignedTechnician = cloud.AssignedTechnician,
                                Status = cloud.Status,
                                LaborFee = cloud.LaborFee,
                                PartsCost = cloud.PartsCost,
                                DepositAmount = cloud.DepositAmount,
                                WarrantyTerms = cloud.WarrantyTerms,
                                CreatedAt = cloud.CreatedAt,
                                CompletedAt = cloud.CompletedAt,
                                PartsSupplier = cloud.PartsSupplier
                            });
                        }
                    }

                    foreach (var local in localRepairs)
                    {
                        if (cloudRepairs.All(r => r.RepairTicketId != local.RepairTicketId && !r.TicketNumber.Equals(local.TicketNumber, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("RepairTicket", local.RepairTicketId.ToString(), local.TicketNumber))
                            {
                                context.RepairTickets.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newRepairs.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "RepairTickets", newRepairs).ConfigureAwait(false);
                    }
                    result.RepairsCount = cloudRepairs.Count;
                }

                // 9. PayrollRecords
                var cloudPayroll = await apiClient.GetPayrollAsync(companyId).ConfigureAwait(false);
                if (cloudPayroll != null)
                {
                    var localPayroll = await context.PayrollRecords.ToListAsync().ConfigureAwait(false);
                    var newPayroll = new List<PayrollRecord>();

                    foreach (var cloud in cloudPayroll)
                    {
                        var local = localPayroll.FirstOrDefault(p => p.PayrollId == cloud.PayrollId);
                        if (local != null)
                        {
                            if (!HasOutbox("PayrollRecord", local.PayrollId.ToString(), cloud.PayrollId.ToString()))
                            {
                                local.StaffId = cloud.StaffId;
                                local.StaffName = cloud.StaffName;
                                local.Role = cloud.Role;
                                local.BaseSalary = cloud.BaseSalary;
                                local.OvertimePay = cloud.OvertimePay;
                                local.CommissionAmount = cloud.CommissionAmount;
                                local.SssDeduction = cloud.SssDeduction;
                                local.PhilHealthDeduction = cloud.PhilHealthDeduction;
                                local.PagIbigDeduction = cloud.PagIbigDeduction;
                                local.WithholdingTax = cloud.WithholdingTax;
                                local.OtherDeductions = cloud.OtherDeductions;
                                local.Deductions = cloud.Deductions;
                                local.Status = cloud.Status;
                                local.PaymentMethod = cloud.PaymentMethod;
                                local.ProcessedBy = cloud.ProcessedBy;
                                local.ProcessedAt = cloud.ProcessedAt;
                                local.PeriodStart = cloud.PeriodStart;
                                local.PeriodEnd = cloud.PeriodEnd;
                            }
                        }
                        else
                        {
                            newPayroll.Add(new PayrollRecord
                            {
                                PayrollId = cloud.PayrollId,
                                StaffId = cloud.StaffId,
                                StaffName = cloud.StaffName,
                                Role = cloud.Role,
                                BaseSalary = cloud.BaseSalary,
                                OvertimePay = cloud.OvertimePay,
                                CommissionAmount = cloud.CommissionAmount,
                                SssDeduction = cloud.SssDeduction,
                                PhilHealthDeduction = cloud.PhilHealthDeduction,
                                PagIbigDeduction = cloud.PagIbigDeduction,
                                WithholdingTax = cloud.WithholdingTax,
                                OtherDeductions = cloud.OtherDeductions,
                                Deductions = cloud.Deductions,
                                Status = cloud.Status,
                                PaymentMethod = cloud.PaymentMethod,
                                ProcessedBy = cloud.ProcessedBy,
                                ProcessedAt = cloud.ProcessedAt,
                                PeriodStart = cloud.PeriodStart,
                                PeriodEnd = cloud.PeriodEnd
                            });
                        }
                    }

                    foreach (var local in localPayroll)
                    {
                        if (cloudPayroll.All(p => p.PayrollId != local.PayrollId))
                        {
                            if (!HasOutbox("PayrollRecord", local.PayrollId.ToString()))
                            {
                                context.PayrollRecords.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newPayroll.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "PayrollRecords", newPayroll).ConfigureAwait(false);
                    }
                    result.PayrollCount = cloudPayroll.Count;
                }

                // 10. ApprovalRequests
                var cloudApprovals = await apiClient.GetApprovalRequestsAsync(companyId).ConfigureAwait(false);
                if (cloudApprovals != null)
                {
                    var localApprovals = await context.ApprovalRequests.ToListAsync().ConfigureAwait(false);
                    var newApprovals = new List<ApprovalRequest>();

                    foreach (var cloud in cloudApprovals)
                    {
                        var local = localApprovals.FirstOrDefault(a => (cloud.RequestId > 0 && a.RequestId == cloud.RequestId) || a.RequestNumber.Equals(cloud.RequestNumber, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("ApprovalRequest", local.RequestId.ToString(), local.RequestNumber, cloud.RequestId.ToString(), cloud.RequestNumber))
                            {
                                local.RequestNumber = cloud.RequestNumber;
                                local.RequestType = cloud.RequestType;
                                local.Title = cloud.Title;
                                local.ReasonDescription = cloud.ReasonDescription;
                                local.RequestedBy = cloud.RequestedBy;
                                local.RequestedAmount = cloud.RequestedAmount;
                                local.Status = cloud.Status;
                                local.ReviewedBy = cloud.ReviewedBy;
                                local.ReviewNotes = cloud.ReviewNotes;
                                local.CreatedAt = cloud.CreatedAt;
                                local.ResolvedAt = cloud.ResolvedAt;
                                if (!string.IsNullOrEmpty(cloud.TargetReferenceId)) local.TargetReferenceId = cloud.TargetReferenceId;
                            }
                        }
                        else
                        {
                            newApprovals.Add(new ApprovalRequest
                            {
                                RequestId = cloud.RequestId,
                                RequestNumber = cloud.RequestNumber,
                                RequestType = cloud.RequestType,
                                Title = cloud.Title,
                                ReasonDescription = cloud.ReasonDescription,
                                RequestedBy = cloud.RequestedBy,
                                RequestedAmount = cloud.RequestedAmount,
                                Status = cloud.Status,
                                ReviewedBy = cloud.ReviewedBy,
                                ReviewNotes = cloud.ReviewNotes,
                                CreatedAt = cloud.CreatedAt,
                                ResolvedAt = cloud.ResolvedAt,
                                TargetReferenceId = cloud.TargetReferenceId
                            });
                        }
                    }

                    foreach (var local in localApprovals)
                    {
                        if (cloudApprovals.All(a => a.RequestId != local.RequestId && !a.RequestNumber.Equals(local.RequestNumber, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("ApprovalRequest", local.RequestId.ToString(), local.RequestNumber))
                            {
                                context.ApprovalRequests.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newApprovals.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "ApprovalRequests", newApprovals).ConfigureAwait(false);
                    }
                    result.ApprovalsCount = cloudApprovals.Count;
                }

                // 11. StorePolicies
                var cloudPolicies = await apiClient.GetPoliciesAsync(companyId).ConfigureAwait(false);
                if (cloudPolicies != null)
                {
                    var localPolicies = await context.StorePolicies.ToListAsync().ConfigureAwait(false);
                    var newPolicies = new List<StorePolicy>();

                    foreach (var cloud in cloudPolicies)
                    {
                        var local = localPolicies.FirstOrDefault(p => (cloud.PolicyId > 0 && p.PolicyId == cloud.PolicyId) || p.PolicyType.Equals(cloud.PolicyType, StringComparison.OrdinalIgnoreCase));
                        if (local != null)
                        {
                            if (!HasOutbox("StorePolicy", local.PolicyId.ToString(), local.PolicyType, cloud.PolicyId.ToString(), cloud.PolicyType))
                            {
                                local.PolicyType = cloud.PolicyType;
                                local.Title = cloud.Title;
                                local.ContentText = cloud.ContentText;
                                local.LastUpdatedBy = cloud.LastUpdatedBy;
                                local.UpdatedAt = cloud.UpdatedAt;
                            }
                        }
                        else
                        {
                            newPolicies.Add(new StorePolicy
                            {
                                PolicyId = cloud.PolicyId,
                                PolicyType = cloud.PolicyType,
                                Title = cloud.Title,
                                ContentText = cloud.ContentText,
                                LastUpdatedBy = cloud.LastUpdatedBy,
                                UpdatedAt = cloud.UpdatedAt
                            });
                        }
                    }

                    foreach (var local in localPolicies)
                    {
                        if (cloudPolicies.All(p => p.PolicyId != local.PolicyId && !p.PolicyType.Equals(local.PolicyType, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!HasOutbox("StorePolicy", local.PolicyId.ToString(), local.PolicyType))
                            {
                                context.StorePolicies.Remove(local);
                            }
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);

                    if (newPolicies.Count > 0)
                    {
                        await InsertWithIdentityAsync(context, "StorePolicies", newPolicies).ConfigureAwait(false);
                    }
                    result.PoliciesCount = cloudPolicies.Count;
                }

                result.Success = true;
                result.Message = "Cloud-to-local synchronization completed successfully.";
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshFromCloudAsync error: {ex.Message}");
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

#pragma warning disable EF1002
        private static async Task InsertWithIdentityAsync<TEntity>(TenantErpDbContext context, string tableName, IEnumerable<TEntity> entities) where TEntity : class
        {
            var list = entities.ToList();
            if (list.Count == 0) return;

            await using var tx = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON;").ConfigureAwait(false);
                context.Set<TEntity>().AddRange(list);
                await context.SaveChangesAsync().ConfigureAwait(false);
                await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF;").ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
#pragma warning restore EF1002
    }

    public class CloudSyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CategoriesCount { get; set; }
        public int ProductsCount { get; set; }
        public int InventoriesCount { get; set; }
        public int CustomersCount { get; set; }
        public int SuppliersCount { get; set; }
        public int StaffCount { get; set; }
        public int OrdersCount { get; set; }
        public int RepairsCount { get; set; }
        public int PayrollCount { get; set; }
        public int ApprovalsCount { get; set; }
        public int PoliciesCount { get; set; }
    }
}
