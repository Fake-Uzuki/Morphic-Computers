using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.infrastructure.data;
using ERP.infrastructure.services;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/[controller]")]
    public class SuppliersController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public SuppliersController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsureSuppliersSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        SupplierId INT IDENTITY(1,1) PRIMARY KEY,
        SupplierCode NVARCHAR(50) NOT NULL,
        SupplierName NVARCHAR(200) NOT NULL,
        ContactPerson NVARCHAR(100) NULL,
        ContactNumber NVARCHAR(50) NULL,
        EmailAddress NVARCHAR(100) NULL,
        Address NVARCHAR(250) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureSuppliersSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers(int companyId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<Supplier>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureSuppliersSchemaAsync(tenantDb, companyId);

                var suppliers = await tenantDb.Suppliers
                    .AsNoTracking()
                    .OrderBy(s => s.SupplierName)
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSuppliers tenantDb error: {ex.Message}");
                return Ok(new List<Supplier>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier(int companyId, [FromBody] Supplier supplier)
        {
            if (supplier == null)
            {
                return BadRequest(new { error = "Supplier payload is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureSuppliersSchemaAsync(tenantDb, companyId);

                if (string.IsNullOrWhiteSpace(supplier.SupplierCode))
                {
                    supplier.SupplierCode = $"SUP-{new Random().Next(1000, 9999)}";
                }

                var existing = await tenantDb.Suppliers
                    .FirstOrDefaultAsync(s => s.SupplierCode == supplier.SupplierCode);
                if (existing != null)
                {
                    existing.SupplierName = supplier.SupplierName;
                    existing.ContactPerson = supplier.ContactPerson;
                    existing.ContactNumber = supplier.ContactNumber;
                    existing.EmailAddress = supplier.EmailAddress;
                    existing.Address = supplier.Address;
                    existing.IsActive = supplier.IsActive;
                    await tenantDb.SaveChangesAsync();
                    return Ok(existing);
                }

                supplier.CreatedAt = DateTime.UtcNow;
                supplier.IsActive = true;

                tenantDb.Suppliers.Add(supplier);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSuppliers), new { companyId }, supplier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save supplier: {ex.Message}" });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSupplier(int companyId, int id, [FromBody] Supplier updated)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureSuppliersSchemaAsync(tenantDb, companyId);

                var supplier = await tenantDb.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == id);
                if (supplier == null)
                {
                    return NotFound(new { error = $"Supplier ID {id} not found." });
                }

                supplier.SupplierName = updated.SupplierName;
                supplier.ContactPerson = updated.ContactPerson;
                supplier.ContactNumber = updated.ContactNumber;
                supplier.EmailAddress = updated.EmailAddress;
                supplier.Address = updated.Address;
                supplier.IsActive = updated.IsActive;

                int affected = await tenantDb.SaveChangesAsync();
                if (affected == 0 && !tenantDb.Entry(supplier).State.HasFlag(EntityState.Unchanged))
                {
                    return StatusCode(500, new { error = "Update failed: 0 rows affected." });
                }
                return Ok(supplier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update supplier: {ex.Message}" });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSupplier(int companyId, int id)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureSuppliersSchemaAsync(tenantDb, companyId);

                var supplier = await tenantDb.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == id);
                if (supplier == null)
                {
                    return NotFound(new { error = $"Supplier ID {id} not found." });
                }

                supplier.IsActive = false; // Soft delete
                await tenantDb.SaveChangesAsync();
                return Ok(new { message = "Supplier deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to delete supplier: {ex.Message}" });
            }
        }
    }
}
