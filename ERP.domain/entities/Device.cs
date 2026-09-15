using System;

namespace ERP.domain.entities
{
    public class Device
    {
        public int DeviceId { get; set; }
        public int CompanyId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation Property
        public Company? Company { get; set; }
    }
}
