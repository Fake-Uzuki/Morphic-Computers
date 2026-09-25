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
    public class CustomersController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public CustomersController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsureCustomersSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE Customers (
        CustomerId INT IDENTITY(1,1) PRIMARY KEY,
        CustomerCode NVARCHAR(50) NOT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
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
                System.Diagnostics.Debug.WriteLine($"EnsureCustomersSchema note: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(int companyId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<Customer>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCustomersSchemaAsync(tenantDb, companyId);

                var customers = await tenantDb.Customers
                    .AsNoTracking()
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCustomers tenantDb error: {ex.Message}");
                return Ok(new List<Customer>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(int companyId, [FromBody] Customer customer)
        {
            if (customer == null)
            {
                return BadRequest(new { error = "Customer payload is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCustomersSchemaAsync(tenantDb, companyId);

                if (string.IsNullOrWhiteSpace(customer.CustomerCode))
                {
                    customer.CustomerCode = $"CUST-{new Random().Next(1000, 9999)}";
                }

                var existing = await tenantDb.Customers
                    .FirstOrDefaultAsync(c => c.CustomerCode == customer.CustomerCode);
                if (existing != null)
                {
                    existing.CustomerName = customer.CustomerName;
                    existing.ContactNumber = customer.ContactNumber;
                    existing.EmailAddress = customer.EmailAddress;
                    existing.Address = customer.Address;
                    existing.IsActive = customer.IsActive;
                    await tenantDb.SaveChangesAsync();
                    return Ok(existing);
                }

                customer.CreatedAt = DateTime.UtcNow;
                customer.IsActive = true;

                tenantDb.Customers.Add(customer);
                await tenantDb.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCustomers), new { companyId }, customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save customer: {ex.Message}" });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int companyId, int id, [FromBody] Customer updated)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCustomersSchemaAsync(tenantDb, companyId);

                var customer = await tenantDb.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
                if (customer == null)
                {
                    return NotFound(new { error = $"Customer ID {id} not found." });
                }

                customer.CustomerName = updated.CustomerName;
                customer.ContactNumber = updated.ContactNumber;
                customer.EmailAddress = updated.EmailAddress;
                customer.Address = updated.Address;
                customer.IsActive = updated.IsActive;

                int affected = await tenantDb.SaveChangesAsync();
                if (affected == 0 && !tenantDb.Entry(customer).State.HasFlag(EntityState.Unchanged))
                {
                    return StatusCode(500, new { error = "Update failed: 0 rows affected." });
                }
                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update customer: {ex.Message}" });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int companyId, int id)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCustomersSchemaAsync(tenantDb, companyId);

                var customer = await tenantDb.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
                if (customer == null)
                {
                    return NotFound(new { error = $"Customer ID {id} not found." });
                }

                customer.IsActive = false;
                await tenantDb.SaveChangesAsync();
                return Ok(new { message = "Customer deactivated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to delete customer: {ex.Message}" });
            }
        }
    }
}
