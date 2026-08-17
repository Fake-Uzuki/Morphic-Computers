namespace IT8_TechStore.Models
{
    public class CartItem
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice => Product.Price;
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
