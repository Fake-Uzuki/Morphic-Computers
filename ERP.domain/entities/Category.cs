namespace ERP.domain.entities
{
    public class Category
    {
        public int Id { get; set; }
        public int CompanyId { get; set; } = 1;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "💻";
        public string Description { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
}
