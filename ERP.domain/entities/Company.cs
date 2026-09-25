using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.domain.security;

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

        // Feature Entitlement Properties (Driven by Centralized ModuleAccessService)
        [NotMapped]
        public bool IsPOSAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "POS");
        [NotMapped]
        public bool IsInventoryAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "Inventory");
        [NotMapped]
        public bool IsRepairAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "Repairs");
        [NotMapped]
        public bool IsSupplierAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "Suppliers");
        [NotMapped]
        public bool IsBusinessIntelligenceAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "BusinessIntelligence");
        [NotMapped]
        public bool IsPayrollAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "Payroll");
        [NotMapped]
        public bool IsBranchAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "BranchManagement");
        [NotMapped]
        public bool IsDashboardAllowed => ModuleAccessService.IsModuleEnabled(PlanName, "Dashboard");

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
