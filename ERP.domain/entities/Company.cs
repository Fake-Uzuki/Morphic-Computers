using System;

namespace ERP.domain.entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Code { get; set; } = "MORPHIC";
        public string Name { get; set; } = "Morphic Computers";
        public string Description { get; set; } = "Main Computer Hardware & IT Store";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString() => Name;
    }
}
