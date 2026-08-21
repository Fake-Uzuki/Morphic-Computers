using System;

namespace IT8_TechStore.Models
{
    /// <summary>
    /// Multi-Tenant Entity representing Company Tenants (Company A, Company B, Company C)
    /// as drawn on the project silos whiteboard architecture.
    /// </summary>
    public class CompanyTenant
    {
        public int Id { get; set; }
        public string TenantCode { get; set; } = string.Empty; // e.g. "COMPANY_A", "COMPANY_B", "COMPANY_C"
        public string CompanyName { get; set; } = string.Empty; // e.g. "Morphic Computers - Main"
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString() => CompanyName;
    }
}
