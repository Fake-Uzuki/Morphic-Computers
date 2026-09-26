using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.domain.entities
{
    public class PurchaseOrderItem
    {
        public int PurchaseOrderItemId { get; set; }
        public int PurchaseOrderId { get; set; }
        public int? ProductId { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalAmount { get; set; }

        [NotMapped]
        public decimal TotalCost
        {
            get => TotalAmount;
            set => TotalAmount = value;
        }

        // Navigation
        [System.Text.Json.Serialization.JsonIgnore]
        public PurchaseOrder? PurchaseOrder { get; set; }

        public Product? Product { get; set; }
    }
}
