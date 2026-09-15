using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.domain.entities
{
    public class Company
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlanName { get; set; } = "Micro";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationship
        public ICollection<Device> Devices { get; set; } = new List<Device>();

        // Feature Entitlement Properties
        [NotMapped]
        public bool IsPOSAllowed => true;
        [NotMapped]
        public bool IsInventoryAllowed => true;
        [NotMapped]
        public bool IsRepairAllowed => !string.Equals(PlanName, "Micro", StringComparison.OrdinalIgnoreCase);
        [NotMapped]
        public bool IsSupplierAllowed => !string.Equals(PlanName, "Micro", StringComparison.OrdinalIgnoreCase);

        // Backward compatibility helpers for WinForms UI
        [NotMapped]
        public int Id { get => CompanyId; set => CompanyId = value; }
        [NotMapped]
        public string Code { get => CompanyCode; set => CompanyCode = value; }
        [NotMapped]
        public string Name { get => CompanyName; set => CompanyName = value; }
        [NotMapped]
        public string Description { get; set; } = string.Empty;

        public override string ToString() => CompanyName;
    }
}
