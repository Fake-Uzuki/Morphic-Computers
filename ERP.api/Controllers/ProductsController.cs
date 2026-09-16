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
    public class ProductsController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredSchemas = new();

        public ProductsController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsureProductsSchemaAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredSchemas.ContainsKey(companyId)) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CategoryName')
BEGIN
    ALTER TABLE Products ADD CategoryName NVARCHAR(100) NOT NULL DEFAULT 'Graphics Cards (GPU)';
END
ELSE
BEGIN
    UPDATE Products SET CategoryName = 'Graphics Cards (GPU)' WHERE CategoryName = 'General' OR CategoryName IS NULL OR CategoryName = '';
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Description')
BEGIN
    ALTER TABLE Products ADD Description NVARCHAR(MAX) NULL;
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ArchivedAt')
BEGIN
    ALTER TABLE Products ADD ArchivedAt DATETIME2 NULL;
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredSchemas.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureProductsSchema note: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all products with live stock quantity for the specified tenant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts(int companyId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<Product>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureProductsSchemaAsync(tenantDb, companyId);

                var products = await tenantDb.Products
                    .AsNoTracking()
                    .OrderBy(x => x.ProductId)
                    .ToListAsync();

                var inventories = await tenantDb.Inventories
                    .AsNoTracking()
                    .ToListAsync();

                // Populate live StockQuantity from Inventory records
                foreach (var p in products)
                {
                    var inv = inventories.FirstOrDefault(i => i.ProductId == p.ProductId);
                    if (inv != null)
                    {
                        p.StockQuantity = (int)inv.QuantityOnHand;
                    }
                }

                // Deduplicate by ProductCode
                var uniqueProducts = products
                    .GroupBy(p => p.ProductCode)
                    .Select(g => g.First())
                    .ToList();

                return Ok(uniqueProducts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProducts tenantDb error: {ex.Message}");
                return Ok(new List<Product>());
            }
        }

        /// <summary>
        /// Adds a new product to the tenant's database and initializes inventory stock.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProduct(int companyId, [FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest(new { error = "Product payload is required." });
            }

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return StatusCode(503, new { error = "Database offline. Network unavailable." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureProductsSchemaAsync(tenantDb, companyId);

                // Check if product code already exists
                var existing = await tenantDb.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == product.ProductCode);

                if (existing != null)
                {
                    // Update existing product instead of creating duplicate
                    existing.ProductName = product.ProductName;
                    existing.UnitPrice = product.UnitPrice;
                    existing.CategoryName = product.CategoryName;
                    existing.Description = product.Description;
                    existing.IsActive = product.IsActive;

                    var inv = await tenantDb.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == existing.ProductId);

                    if (inv != null)
                    {
                        inv.QuantityOnHand = product.StockQuantity;
                        inv.LastUpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        tenantDb.Inventories.Add(new Inventory
                        {
                            ProductId = existing.ProductId,
                            QuantityOnHand = product.StockQuantity,
                            ReorderLevel = 3,
                            LastUpdatedAt = DateTime.UtcNow
                        });
                    }

                    await tenantDb.SaveChangesAsync();
                    product.ProductId = existing.ProductId;
                    return Ok(product);
                }

                // Ensure ProductId is 0 for database identity generation
                product.ProductId = 0;
                tenantDb.Products.Add(product);
                await tenantDb.SaveChangesAsync();

                // Create corresponding Inventory record
                var newInv = new Inventory
                {
                    ProductId = product.ProductId,
                    QuantityOnHand = product.StockQuantity,
                    ReorderLevel = 3,
                    LastUpdatedAt = DateTime.UtcNow
                };
                tenantDb.Inventories.Add(newInv);
                await tenantDb.SaveChangesAsync();

                return Created($"/api/tenant/{companyId}/products/{product.ProductId}", product);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateProduct tenantDb error: {ex.Message}");
                return StatusCode(503, new { error = $"Database offline or unreachable: {ex.Message}" });
            }
        }

        /// <summary>
        /// Updates an existing product and its inventory stock.
        /// </summary>
        [HttpPut("{productId:int}")]
        public async Task<IActionResult> UpdateProduct(int companyId, int productId, [FromBody] Product product)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return StatusCode(503, new { error = "Database offline. Network unavailable." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureProductsSchemaAsync(tenantDb, companyId);

                var dbProduct = await tenantDb.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId || p.ProductCode == product.ProductCode);

                if (dbProduct == null)
                {
                    return NotFound(new { error = $"Product with ID {productId} not found." });
                }

                dbProduct.ProductName = product.ProductName;
                dbProduct.ProductCode = product.ProductCode;
                dbProduct.UnitPrice = product.UnitPrice;
                dbProduct.CategoryName = product.CategoryName;
                dbProduct.Description = product.Description;
                dbProduct.IsActive = product.IsActive;

                var inv = await tenantDb.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == dbProduct.ProductId);

                if (inv != null)
                {
                    inv.QuantityOnHand = product.StockQuantity;
                    inv.LastUpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    tenantDb.Inventories.Add(new Inventory
                    {
                        ProductId = dbProduct.ProductId,
                        QuantityOnHand = product.StockQuantity,
                        ReorderLevel = 3,
                        LastUpdatedAt = DateTime.UtcNow
                    });
                }

                await tenantDb.SaveChangesAsync();
                return Ok(dbProduct);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProduct tenantDb error: {ex.Message}");
                return StatusCode(503, new { error = $"Database offline or unreachable: {ex.Message}" });
            }
        }

        /// <summary>
        /// Enterprise Soft Delete: Archives product and marks inactive. Preserves order history.
        /// </summary>
        [HttpPut("{productId:int}/archive")]
        public async Task<IActionResult> ArchiveProduct(int companyId, int productId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return StatusCode(503, new { error = "Database offline. Network unavailable." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureProductsSchemaAsync(tenantDb, companyId);

                var dbProduct = await tenantDb.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                if (dbProduct == null)
                {
                    return NotFound(new { error = $"Product with ID {productId} not found." });
                }

                dbProduct.IsActive = false;
                dbProduct.ArchivedAt = DateTime.UtcNow;

                await tenantDb.SaveChangesAsync();
                return Ok(new { success = true, message = $"Product #{productId} archived successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ArchiveProduct tenantDb error: {ex.Message}");
                return StatusCode(503, new { error = $"Database offline or unreachable: {ex.Message}" });
            }
        }

        /// <summary>
        /// Restores an archived product back to active catalog.
        /// </summary>
        [HttpPut("{productId:int}/restore")]
        public async Task<IActionResult> RestoreProduct(int companyId, int productId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return StatusCode(503, new { error = "Database offline. Network unavailable." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureProductsSchemaAsync(tenantDb, companyId);

                var dbProduct = await tenantDb.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                if (dbProduct == null)
                {
                    return NotFound(new { error = $"Product with ID {productId} not found." });
                }

                dbProduct.IsActive = true;
                dbProduct.ArchivedAt = null;

                await tenantDb.SaveChangesAsync();
                return Ok(new { success = true, message = $"Product #{productId} restored successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreProduct tenantDb error: {ex.Message}");
                return StatusCode(503, new { error = $"Database offline or unreachable: {ex.Message}" });
            }
        }

        /// <summary>
        /// Enterprise Soft Delete via DELETE verb to preserve historical orders.
        /// </summary>
        [HttpDelete("{productId:int}")]
        public async Task<IActionResult> DeleteProduct(int companyId, int productId)
        {
            return await ArchiveProduct(companyId, productId);
        }
    }
}
