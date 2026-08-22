using System;

namespace ERP.domain.entities
{
    public class CompanyDatabase
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string DatabaseName { get; set; } = "MorphicComputersDB";
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
