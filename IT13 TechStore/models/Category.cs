namespace IT8_TechStore.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "💻";
        public string Description { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
}
