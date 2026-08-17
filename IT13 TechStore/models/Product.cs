namespace IT8_TechStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "General";
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Description { get; set; } = string.Empty;

        public bool IsLowStock => StockQuantity <= 5;
        public string StockStatus => StockQuantity switch
        {
            0 => "Out of Stock",
            <= 5 => $"Low Stock ({StockQuantity})",
            _ => $"In Stock ({StockQuantity})"
        };
    }
}
