using System;

namespace ERP.domain.entities
{
    public class StaffMember
    {
        public int StaffId { get; set; }
        public int CompanyId { get; set; } = 2;
        public string StaffCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        
        // Roles: Store Administrator, Store Manager, Hardware Technician, Cashier Operations
        public string Role { get; set; } = "Hardware Technician";
        public string PositionTitle { get; set; } = "Hardware Technician";
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public decimal HourlyRate { get; set; } = 150.00m;
        public decimal MonthlySalary { get; set; } = 25000.00m;
        public bool IsActive { get; set; } = true;
        public DateTime HiredDate { get; set; } = DateTime.UtcNow;
    }
}
