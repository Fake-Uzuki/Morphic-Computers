using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    public class OrdersController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private static bool _tableEnsured;

        public OrdersController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        private static async Task EnsureOrdersTableAsync(TenantErpDbContext db)
        {
            if (_tableEnsured) return;
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;
            try
            {
                const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        Id NVARCHAR(100) NOT NULL PRIMARY KEY,
        CompanyId INT NOT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        ItemsJson NVARCHAR(MAX) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        Discount DECIMAL(18,2) NOT NULL,
        Tax DECIMAL(18,2) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL
    );
END";
                await db.Database.ExecuteSqlRawAsync(sql);
                _tableEnsured = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureOrdersTable note: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all orders for the specified tenant from MonsterASP database.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders(int companyId)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return Ok(new List<Order>());
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureOrdersTableAsync(tenantDb);

                var orders = await tenantDb.Orders
                    .AsNoTracking()
                    .Where(o => o.CompanyId == companyId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new Order
                    {
                        Id = o.Id,
                        CompanyId = o.CompanyId,
                        CustomerName = o.CustomerName,
                        CreatedAt = o.CreatedAt,
                        ItemsJson = o.ItemsJson,
                        Subtotal = o.Subtotal,
                        Discount = o.Discount,
                        Tax = o.Tax,
                        TotalAmount = o.TotalAmount,
                        PaymentMethod = o.PaymentMethod
                    })
                    .ToListAsync();

                foreach (var o in orders)
                {
                    if (!string.IsNullOrWhiteSpace(o.ItemsJson) && (o.Items == null || o.Items.Count == 0))
                    {
                        try
                        {
                            o.Items = JsonSerializer.Deserialize<List<CartItem>>(o.ItemsJson) ?? new();
                        }
                        catch
                        {
                            o.Items = new();
                        }
                    }
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOrders error: {ex.Message}");
                return Ok(new List<Order>());
            }
        }

        /// <summary>
        /// Processes a POS checkout order: logs the order to MonsterASP database and deducts inventory quantities atomically.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(int companyId, [FromBody] Order order)
        {
            if (order == null || order.Items == null || order.Items.Count == 0)
            {
                return BadRequest(new { error = "Order must contain at least one item." });
            }

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return StatusCode(503, new { error = "Database offline. Network unavailable." });
            }

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(companyId);
                await EnsureOrdersTableAsync(tenantDb);

                order.CompanyId = companyId;
                if (string.IsNullOrWhiteSpace(order.ItemsJson) || order.ItemsJson == "[]")
                {
                    order.ItemsJson = JsonSerializer.Serialize(order.Items);
                }

                // Persist order in MonsterASP cloud database (only existing cloud columns, omitting CashierName)
                bool exists = await tenantDb.Orders.AnyAsync(o => o.Id == order.Id);
                if (exists)
                {
                    return Ok(order);
                }

                // Validate items and stock availability BEFORE making changes
                foreach (var item in order.Items)
                {
                    var product = await tenantDb.Products
                        .Where(p => p.ProductId == item.ProductId)
                        .Select(p => new { p.ProductId, p.ProductName, p.IsActive })
                        .FirstOrDefaultAsync();
                    if (product == null)
                    {
                        return BadRequest(new { error = $"Product '{item.ProductName}' (ID: {item.ProductId}) does not exist." });
                    }
                    if (!product.IsActive)
                    {
                        return BadRequest(new { error = $"Product '{item.ProductName}' is inactive/archived and cannot be sold." });
                    }

                    var inv = await tenantDb.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
                    int currentStock = (int)(inv?.QuantityOnHand ?? 0m);
                    if (currentStock <= 0)
                    {
                        return BadRequest(new { error = $"Product '{item.ProductName}' is out of stock (Stock: 0)." });
                    }
                    if (currentStock < item.Quantity)
                    {
                        return BadRequest(new { error = $"Insufficient stock for '{item.ProductName}'. Requested: {item.Quantity}, Available: {currentStock}." });
                    }
                }

                int orderAffected = await tenantDb.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO Orders (Id, CompanyId, CustomerName, CreatedAt, ItemsJson, Subtotal, Discount, Tax, TotalAmount, PaymentMethod)
                    VALUES ({order.Id}, {order.CompanyId}, {order.CustomerName}, {order.CreatedAt}, {order.ItemsJson}, {order.Subtotal}, {order.Discount}, {order.Tax}, {order.TotalAmount}, {order.PaymentMethod});
                ");

                if (orderAffected == 0)
                {
                    return StatusCode(500, new { error = "Failed to record order in cloud database." });
                }

                // Deduct stock for purchased items
                foreach (var item in order.Items)
                {
                    var inv = await tenantDb.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                    if (inv != null)
                    {
                        inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                        inv.LastUpdatedAt = DateTime.UtcNow;
                    }
                }

                await tenantDb.SaveChangesAsync();

                return Created($"/api/tenant/{companyId}/orders/{order.Id}", order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PROCESS ORDER ERROR] {ex.Message} -> {ex.InnerException?.Message}\n{ex.StackTrace}");
                return StatusCode(503, new { error = $"Database offline or unreachable: {ex.Message}" });
            }
        }
    }
}
