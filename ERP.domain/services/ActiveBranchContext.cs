using System;

namespace ERP.domain.services
{
    public class ActiveBranchContext
    {
        public int CompanyId { get; set; }
        public int? BranchId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public bool IsAllBranches { get; set; }

        public bool HasSpecificBranch => BranchId.HasValue && BranchId.Value > 0 && !IsAllBranches;

        public void Clear()
        {
            CompanyId = 0;
            BranchId = null;
            BranchCode = null;
            BranchName = null;
            IsAllBranches = false;
        }

        public void SetBranch(int companyId, int branchId, string branchCode, string branchName)
        {
            CompanyId = companyId;
            BranchId = branchId;
            BranchCode = branchCode;
            BranchName = branchName;
            IsAllBranches = false;
        }

        public void SetAllBranches(int companyId)
        {
            CompanyId = companyId;
            BranchId = null;
            BranchCode = null;
            BranchName = "All Branches";
            IsAllBranches = true;
        }

        public override string ToString()
        {
            if (IsAllBranches) return "All Branches";
            if (HasSpecificBranch) return $"{BranchName} ({BranchCode})";
            return "No Branch Selected";
        }
    }
}
