namespace IT8_TechStore.Models
{
    public class CartItem
    {
        public Product Product { get; set; } = null!;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public decimal Subtotal => TotalPrice;
    }
}
