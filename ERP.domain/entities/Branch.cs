using System;

namespace ERP.domain.entities
{
    public class Branch
    {
        public int BranchId { get; set; }
        public int CompanyId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public int AssignedStaffCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // UI helper properties for backward compatibility with existing BranchManagementView
        public string CityLocation => City;
        public string Status => IsActive ? "Active" : "Archived";
    }
}
