using System;

namespace ERP.domain.entities
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal ReorderLevel { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
        public Product? Product { get; set; }
    }
}
