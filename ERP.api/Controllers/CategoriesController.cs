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
    public class CategoriesController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _ensuredTables = new();

        public CategoriesController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static readonly HashSet<string> DefaultPresetNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Graphics Cards (GPU)",
            "Processors (CPU)",
            "Memory (RAM)",
            "Storage (SSD/HDD)",
            "Peripherals"
        };

        private static async Task EnsureCategoriesTableAsync(TenantErpDbContext db, int companyId)
        {
            if (_ensuredTables.ContainsKey(companyId)) return;
            try
            {
                string sql = $@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Icon NVARCHAR(50) NULL,
        Description NVARCHAR(500) NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM Categories WHERE CompanyId = {companyId})
BEGIN
    INSERT INTO Categories (CompanyId, Name, Description) VALUES
    ({companyId}, 'Graphics Cards (GPU)', 'Gaming & Productivity GPUs'),
    ({companyId}, 'Processors (CPU)', 'Intel & AMD Processors'),
    ({companyId}, 'Memory (RAM)', 'DDR4 & DDR5 RAM Kits'),
    ({companyId}, 'Storage (SSD/HDD)', 'NVMe SSDs & Hard Drives'),
    ({companyId}, 'Peripherals', 'Keyboards, Mice & Headsets');
END
";
                await db.Database.ExecuteSqlRawAsync(sql);
                _ensuredTables.TryAdd(companyId, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureCategoriesTable note: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all categories for the specified tenant from MonsterASP database.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories(int companyId)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCategoriesTableAsync(tenantDb, companyId);

                var categories = await tenantDb.Categories
                    .AsNoTracking()
                    .Where(c => c.CompanyId == companyId)
                    .OrderBy(c => c.Id)
                    .ToListAsync();

                if (categories.Count == 0)
                {
                    // Fallback to standard 5
                    return Ok(GetDefaultCategories(companyId));
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CATEGORIES GET ERROR] {ex.Message} -> {ex.InnerException?.Message}\n{ex.StackTrace}");
                return Ok(GetDefaultCategories(companyId));
            }
        }

        /// <summary>
        /// Adds a new product category to the tenant database.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory(int companyId, [FromBody] Category category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest(new { error = "Category name is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCategoriesTableAsync(tenantDb, companyId);

                category.Name = category.Name.Trim();
                category.CompanyId = companyId;

                var existing = await tenantDb.Categories
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Name.ToLower() == category.Name.ToLower());

                if (existing != null)
                {
                    return Ok(existing);
                }

                category.Id = 0;
                tenantDb.Categories.Add(category);
                await tenantDb.SaveChangesAsync();

                return Created($"/api/tenant/{companyId}/categories/{category.Id}", category);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCategory error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Renames or updates a product category, cascading the new name to any existing products.
        /// </summary>
        [HttpPut("{categoryId:int}")]
        public async Task<IActionResult> UpdateCategory(int companyId, int categoryId, [FromBody] Category update)
        {
            if (update == null || string.IsNullOrWhiteSpace(update.Name))
            {
                return BadRequest(new { error = "Category name is required." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCategoriesTableAsync(tenantDb, companyId);

                var existing = await tenantDb.Categories
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == categoryId);

                if (existing == null)
                {
                    return NotFound(new { error = $"Category with ID {categoryId} not found." });
                }

                string oldName = existing.Name;
                string newName = update.Name.Trim();

                // Prevent duplicates
                bool isDuplicate = await tenantDb.Categories
                    .AnyAsync(c => c.CompanyId == companyId && c.Id != categoryId && c.Name.ToLower() == newName.ToLower());
                if (isDuplicate)
                {
                    return Conflict(new { error = $"A category named '{newName}' already exists." });
                }

                existing.Name = newName;
                if (!string.IsNullOrEmpty(update.Description))
                {
                    existing.Description = update.Description.Trim();
                }

                await tenantDb.SaveChangesAsync();

                // Cascade update to Products
                if (!oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                {
                    await tenantDb.Database.ExecuteSqlRawAsync(
                        "UPDATE Products SET CategoryName = {0} WHERE CategoryName = {1}",
                        newName, oldName);
                }

                return Ok(existing);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCategory error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a custom category, safely reassigning any products using it to a fallback category.
        /// Protected default system presets cannot be deleted.
        /// </summary>
        [HttpDelete("{categoryId:int}")]
        public async Task<IActionResult> DeleteCategory(int companyId, int categoryId, [FromQuery] string? reassignTo = null)
        {
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureCategoriesTableAsync(tenantDb, companyId);

                var existing = await tenantDb.Categories
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == categoryId);

                if (existing == null)
                {
                    return NotFound(new { error = $"Category with ID {categoryId} not found." });
                }

                // Check default preset protection
                if (DefaultPresetNames.Contains(existing.Name))
                {
                    return BadRequest(new { error = "Default system preset categories cannot be deleted." });
                }

                string fallback = string.IsNullOrWhiteSpace(reassignTo) ? "Graphics Cards (GPU)" : reassignTo.Trim();

                // Reassign existing products before deleting
                await tenantDb.Database.ExecuteSqlRawAsync(
                    "UPDATE Products SET CategoryName = {0} WHERE CategoryName = {1}",
                    fallback, existing.Name);

                tenantDb.Categories.Remove(existing);
                await tenantDb.SaveChangesAsync();

                return Ok(new { success = true, deletedId = categoryId, reassignedTo = fallback });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteCategory error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private static List<Category> GetDefaultCategories(int companyId) => new()
        {
            new Category { Id = 1, CompanyId = companyId, Name = "Graphics Cards (GPU)", Description = "Gaming & Productivity GPUs" },
            new Category { Id = 2, CompanyId = companyId, Name = "Processors (CPU)", Description = "Intel & AMD Processors" },
            new Category { Id = 3, CompanyId = companyId, Name = "Memory (RAM)", Description = "DDR4 & DDR5 RAM Kits" },
            new Category { Id = 4, CompanyId = companyId, Name = "Storage (SSD/HDD)", Description = "NVMe SSDs & Hard Drives" },
            new Category { Id = 5, CompanyId = companyId, Name = "Peripherals", Description = "Keyboards, Mice & Headsets" }
        };
    }
}
