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
                    .Select(p => new Product
                    {
                        ProductId = p.ProductId,
                        ProductCode = p.ProductCode,
                        ProductName = p.ProductName,
                        UnitPrice = p.UnitPrice,
                        CategoryName = p.CategoryName,
                        Description = p.Description,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        ArchivedAt = p.ArchivedAt
                    })
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

                // Check if product code already exists (projecting only ProductId to avoid unmigrated cloud columns)
                var existing = await tenantDb.Products
                    .Where(p => p.ProductCode == product.ProductCode)
                    .Select(p => new { p.ProductId })
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    // Update existing product without touching local-only cloud-absent columns
                    await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE Products
                        SET ProductName = {product.ProductName},
                            UnitPrice = {product.UnitPrice},
                            CategoryName = {product.CategoryName},
                            Description = {product.Description},
                            IsActive = {product.IsActive}
                        WHERE ProductId = {existing.ProductId};
                    ");

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

                // Insert into cloud Products table with only cloud-existing columns
                product.CreatedAt = DateTime.UtcNow;
                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO Products (ProductCode, ProductName, UnitPrice, CategoryName, Description, IsActive, CreatedAt)
                    VALUES ({product.ProductCode}, {product.ProductName}, {product.UnitPrice}, {product.CategoryName}, {product.Description}, {product.IsActive}, {product.CreatedAt});
                ");

                int newId = await tenantDb.Products
                    .Where(p => p.ProductCode == product.ProductCode)
                    .Select(p => p.ProductId)
                    .FirstAsync();
                product.ProductId = newId;

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

                var target = await tenantDb.Products
                    .Where(p => p.ProductId == productId || p.ProductCode == product.ProductCode)
                    .Select(p => new { p.ProductId })
                    .FirstOrDefaultAsync();

                if (target == null)
                {
                    return NotFound(new { error = $"Product with ID {productId} not found." });
                }

                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Products
                    SET ProductName = {product.ProductName},
                        ProductCode = {product.ProductCode},
                        UnitPrice = {product.UnitPrice},
                        CategoryName = {product.CategoryName},
                        Description = {product.Description},
                        IsActive = {product.IsActive}
                    WHERE ProductId = {target.ProductId};
                ");

                var inv = await tenantDb.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == target.ProductId);

                if (inv != null)
                {
                    inv.QuantityOnHand = product.StockQuantity;
                    inv.LastUpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    tenantDb.Inventories.Add(new Inventory
                    {
                        ProductId = target.ProductId,
                        QuantityOnHand = product.StockQuantity,
                        ReorderLevel = 3,
                        LastUpdatedAt = DateTime.UtcNow
                    });
                }

                await tenantDb.SaveChangesAsync();
                product.ProductId = target.ProductId;
                return Ok(product);
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

                bool exists = await tenantDb.Products.AnyAsync(p => p.ProductId == productId);
                if (!exists)
                {
                    return NotFound(new { error = $"Product with ID {productId} not found." });
                }

                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Products
                    SET IsActive = 0,
                        ArchivedAt = {DateTime.UtcNow}
                    WHERE ProductId = {productId};
                ");

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

                bool exists = await tenantDb.Products.AnyAsync(p => p.ProductId == productId);
                if (!exists)
                {
                    return NotFound(new { error = $"Product with ID {productId} not found." });
                }

                await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Products
                    SET IsActive = 1,
                        ArchivedAt = NULL
                    WHERE ProductId = {productId};
                ");

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
