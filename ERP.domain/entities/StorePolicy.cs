using System;

namespace ERP.domain.entities
{
    public class StorePolicy
    {
        public int PolicyId { get; set; }
        public int CompanyId { get; set; } = 2;
        public string PolicyType { get; set; } = "RepairLiabilityWaiver"; // RepairLiabilityWaiver, 30DayWarrantyTerms, ReturnAndRefundPolicy, DataPrivacyNotice
        public string Title { get; set; } = string.Empty;
        public string ContentText { get; set; } = string.Empty;
        public string LastUpdatedBy { get; set; } = "Admin";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
