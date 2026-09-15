using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.domain.entities
{
    public class CompanyDatabase
    {
        public int CompanyDatabaseId { get; set; }
        public int CompanyId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string CredentialKey { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation Property
        public Company? Company { get; set; }

        // Backward compatibility helper
        [NotMapped]
        public int Id { get => CompanyDatabaseId; set => CompanyDatabaseId = value; }
    }
}
