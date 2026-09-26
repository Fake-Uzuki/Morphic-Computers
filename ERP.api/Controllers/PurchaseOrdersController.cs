using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;
using ERP.infrastructure.services;
using ERP.domain.security;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterErpDbContext _masterDb;

        public PurchaseOrdersController(ITenantDbContextFactory tenantFactory, MasterErpDbContext masterDb)
        {
            _tenantFactory = tenantFactory;
            _masterDb = masterDb;
        }

        private async Task<(bool Allowed, string? ErrorMessage, int StatusCode)> CheckPlanAccessAsync(int companyId)
        {
            if (companyId <= 0)
            {
                return (false, "Super Admin or platform-level callers cannot perform tenant operational procurement.", 403);
            }

            var company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                return (false, $"Company ID {companyId} not found.", 404);
            }

            if (!ModuleAccessService.IsModuleEnabled(company.PlanName, "Procurement"))
            {
                return (false, $"Plan '{company.PlanName}' does not include access to the Procurement module. Upgrade to Medium to enable Procurement.", 403);
            }

            return (true, null, 200);
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrders(int companyId, [FromQuery] bool includeArchived = true)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var query = tenantDb.PurchaseOrders
                    .AsNoTracking()
                    .Include(po => po.Items)
                    .Where(po => po.CompanyId == companyId);

                if (!includeArchived)
                {
                    query = query.Where(po => po.IsActive);
                }

                var list = await query.OrderByDescending(po => po.OrderDate).ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPurchaseOrders error: {ex.Message}");
                return StatusCode(500, new { error = "Failed to load purchase orders from tenant database." });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPurchaseOrderById(int companyId, int id)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var po = await tenantDb.PurchaseOrders
                    .AsNoTracking()
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == id && p.CompanyId == companyId);

                if (po == null)
                {
                    return NotFound(new { error = $"Purchase Order ID {id} not found for this company." });
                }

                return Ok(po);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder(int companyId, [FromBody] PurchaseOrder order)
        {
            if (order == null)
            {
                return BadRequest(new { error = "Purchase order payload is required." });
            }

            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);

                // 1. Validate Supplier exists
                var supplier = await tenantDb.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == order.SupplierId);
                if (supplier == null)
                {
                    // Fallback attempt: match by name if SupplierId was not set
                    if (!string.IsNullOrWhiteSpace(order.SupplierName))
                    {
                        supplier = await tenantDb.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierName == order.SupplierName.Trim());
                    }
                }

                if (supplier == null)
                {
                    return BadRequest(new { error = $"Supplier ID {order.SupplierId} not found in current tenant database." });
                }

                order.SupplierId = supplier.SupplierId;
                order.SupplierName = supplier.SupplierName;

                // 2. Validate Items
                if (order.Items == null || order.Items.Count == 0)
                {
                    return BadRequest(new { error = "Purchase order must contain at least one line item." });
                }

                decimal calculatedTotal = 0;
                foreach (var item in order.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        return BadRequest(new { error = $"Item '{item.ItemDescription}' quantity must be greater than zero." });
                    }

                    if (item.UnitCost < 0)
                    {
                        return BadRequest(new { error = $"Item '{item.ItemDescription}' unit cost cannot be negative." });
                    }

                    if (item.ProductId.HasValue && item.ProductId.Value > 0)
                    {
                        var product = await tenantDb.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == item.ProductId.Value);
                        if (product == null)
                        {
                            return BadRequest(new { error = $"Product ID {item.ProductId.Value} does not exist in current tenant database." });
                        }

                        if (string.IsNullOrWhiteSpace(item.ItemDescription))
                        {
                            item.ItemDescription = product.ProductName;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(item.ItemDescription))
                    {
                        return BadRequest(new { error = "Item description cannot be empty." });
                    }

                    item.TotalAmount = item.Quantity * item.UnitCost;
                    calculatedTotal += item.TotalAmount;
                }

                order.TotalAmount = calculatedTotal;

                // 3. Purchase Order Number generation / uniqueness
                if (string.IsNullOrWhiteSpace(order.PurchaseOrderNumber))
                {
                    int existingCount = await tenantDb.PurchaseOrders.CountAsync(p => p.CompanyId == companyId);
                    order.PurchaseOrderNumber = $"PO-{DateTime.UtcNow:yyyy}-{(existingCount + 1):D3}";
                }
                else
                {
                    order.PurchaseOrderNumber = order.PurchaseOrderNumber.Trim();
                }

                bool poNumberExists = await tenantDb.PurchaseOrders.AnyAsync(p => p.CompanyId == companyId && p.PurchaseOrderNumber == order.PurchaseOrderNumber);
                if (poNumberExists)
                {
                    return Conflict(new { error = $"A purchase order with number '{order.PurchaseOrderNumber}' already exists for this company." });
                }

                // 4. Persistence
                order.CompanyId = companyId;
                order.PurchaseOrderId = 0;
                order.CreatedAt = DateTime.UtcNow;
                if (order.OrderDate == default) order.OrderDate = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(order.Status)) order.Status = "In Transit";
                order.IsActive = true;

                foreach (var item in order.Items)
                {
                    item.PurchaseOrderId = 0;
                    item.PurchaseOrderItemId = 0;
                }

                tenantDb.PurchaseOrders.Add(order);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPurchaseOrderById), new { companyId, id = order.PurchaseOrderId }, order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePurchaseOrder(int companyId, int id, [FromBody] PurchaseOrder order)
        {
            if (order == null)
            {
                return BadRequest(new { error = "Purchase order payload is required." });
            }

            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var existing = await tenantDb.PurchaseOrders
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == id && p.CompanyId == companyId);

                if (existing == null)
                {
                    return NotFound(new { error = $"Purchase Order ID {id} not found." });
                }

                // Validate PO Number uniqueness if changed
                string newPoNumber = string.IsNullOrWhiteSpace(order.PurchaseOrderNumber) ? existing.PurchaseOrderNumber : order.PurchaseOrderNumber.Trim();
                if (!string.Equals(existing.PurchaseOrderNumber, newPoNumber, StringComparison.OrdinalIgnoreCase))
                {
                    bool poNumberExists = await tenantDb.PurchaseOrders.AnyAsync(p => p.CompanyId == companyId && p.PurchaseOrderId != id && p.PurchaseOrderNumber == newPoNumber);
                    if (poNumberExists)
                    {
                        return Conflict(new { error = $"A purchase order with number '{newPoNumber}' already exists for this company." });
                    }
                    existing.PurchaseOrderNumber = newPoNumber;
                }

                // Validate supplier if changed
                if (order.SupplierId > 0 && order.SupplierId != existing.SupplierId)
                {
                    var supplier = await tenantDb.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == order.SupplierId);
                    if (supplier == null)
                    {
                        return BadRequest(new { error = $"Supplier ID {order.SupplierId} not found in current tenant database." });
                    }
                    existing.SupplierId = supplier.SupplierId;
                    existing.SupplierName = supplier.SupplierName;
                }
                else if (!string.IsNullOrWhiteSpace(order.SupplierName))
                {
                    existing.SupplierName = order.SupplierName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(order.Status))
                {
                    existing.Status = order.Status.Trim();
                }

                if (order.ExpectedDeliveryDate.HasValue)
                {
                    existing.ExpectedDeliveryDate = order.ExpectedDeliveryDate;
                }

                if (order.ReceivedDate.HasValue)
                {
                    existing.ReceivedDate = order.ReceivedDate;
                }

                if (order.Notes != null) existing.Notes = order.Notes;
                if (order.ApprovedBy != null) existing.ApprovedBy = order.ApprovedBy;
                existing.IsActive = order.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update items if provided
                if (order.Items != null && order.Items.Count > 0)
                {
                    decimal updatedTotal = 0;
                    foreach (var item in order.Items)
                    {
                        if (item.Quantity <= 0)
                        {
                            return BadRequest(new { error = $"Item '{item.ItemDescription}' quantity must be greater than zero." });
                        }

                        if (item.UnitCost < 0)
                        {
                            return BadRequest(new { error = $"Item '{item.ItemDescription}' unit cost cannot be negative." });
                        }

                        if (item.ProductId.HasValue && item.ProductId.Value > 0)
                        {
                            var product = await tenantDb.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == item.ProductId.Value);
                            if (product == null)
                            {
                                return BadRequest(new { error = $"Product ID {item.ProductId.Value} does not exist in current tenant database." });
                            }
                        }

                        item.TotalAmount = item.Quantity * item.UnitCost;
                        updatedTotal += item.TotalAmount;
                    }

                    // Replace items
                    tenantDb.PurchaseOrderItems.RemoveRange(existing.Items);
                    existing.Items.Clear();

                    foreach (var item in order.Items)
                    {
                        item.PurchaseOrderId = existing.PurchaseOrderId;
                        item.PurchaseOrderItemId = 0;
                        existing.Items.Add(item);
                    }

                    existing.TotalAmount = updatedTotal;
                }

                await tenantDb.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> ArchivePurchaseOrder(int companyId, int id)
        {
            var access = await CheckPlanAccessAsync(companyId);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new { error = access.ErrorMessage });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                var existing = await tenantDb.PurchaseOrders.FirstOrDefaultAsync(p => p.PurchaseOrderId == id && p.CompanyId == companyId);
                if (existing == null)
                {
                    return NotFound(new { error = $"Purchase Order ID {id} not found." });
                }

                existing.IsActive = !existing.IsActive;
                if (!existing.IsActive)
                {
                    existing.Status = "Cancelled";
                }
                existing.UpdatedAt = DateTime.UtcNow;

                await tenantDb.SaveChangesAsync();
                return Ok(new { message = $"Purchase Order {existing.PurchaseOrderNumber} active status set to {existing.IsActive}.", isActive = existing.IsActive, status = existing.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
