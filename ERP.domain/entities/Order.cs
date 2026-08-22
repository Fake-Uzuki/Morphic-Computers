using System;
using System.Collections.Generic;

namespace ERP.domain.entities
{
    public class Order
    {
        public string Id { get; set; } = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        public int CompanyId { get; set; } = 1;
        public string CustomerName { get; set; } = "Walk-in Customer";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }
}
