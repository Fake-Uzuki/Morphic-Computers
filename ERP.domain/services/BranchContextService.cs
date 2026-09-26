using System;
using System.Collections.Generic;
using System.Linq;
using ERP.domain.entities;

namespace ERP.domain.services
{
    public class BranchContextService
    {
        private static readonly Lazy<BranchContextService> _instance = new(() => new BranchContextService());
        public static BranchContextService Instance => _instance.Value;

        private readonly ActiveBranchContext _currentContext = new();
        private readonly object _lock = new();

        public event Action? ActiveBranchChanged;
        public event Action<ActiveBranchContext>? ContextChanged;

        private void NotifyChanged()
        {
            ActiveBranchChanged?.Invoke();
            ContextChanged?.Invoke(CurrentContext);
        }

        public ActiveBranchContext CurrentContext
        {
            get
            {
                lock (_lock)
                {
                    return new ActiveBranchContext
                    {
                        CompanyId = _currentContext.CompanyId,
                        BranchId = _currentContext.BranchId,
                        BranchCode = _currentContext.BranchCode,
                        BranchName = _currentContext.BranchName,
                        IsAllBranches = _currentContext.IsAllBranches
                    };
                }
            }
        }

        public int CurrentCompanyId => _currentContext.CompanyId;
        public int? CurrentBranchId => _currentContext.BranchId;
        public string? CurrentBranchCode => _currentContext.BranchCode;
        public string? CurrentBranchName => _currentContext.BranchName;
        public bool IsAllBranches => _currentContext.IsAllBranches;
        public bool HasSpecificBranch => _currentContext.HasSpecificBranch;

        /// <summary>
        /// Synchronizes and updates the branch context for a tenant's branches.
        /// If only 1 active branch exists, it is automatically selected.
        /// If the currently selected branch was deactivated, it falls back or clears.
        /// </summary>
        public void SyncBranches(int companyId, IEnumerable<Branch>? branches, bool preserveSelectionIfValid = true)
        {
            lock (_lock)
            {
                if (companyId <= 0)
                {
                    Clear();
                    return;
                }

                bool companyChanged = _currentContext.CompanyId != companyId;
                if (companyChanged)
                {
                    _currentContext.Clear();
                    _currentContext.CompanyId = companyId;
                }

                var activeBranches = (branches ?? Enumerable.Empty<Branch>())
                    .Where(b => b.CompanyId == companyId && b.IsActive)
                    .ToList();

                if (activeBranches.Count == 0)
                {
                    _currentContext.Clear();
                    _currentContext.CompanyId = companyId;
                    NotifyChanged();
                    return;
                }

                if (activeBranches.Count == 1)
                {
                    var single = activeBranches[0];
                    bool changed = _currentContext.BranchId != single.BranchId;
                    _currentContext.SetBranch(companyId, single.BranchId, single.BranchCode, single.BranchName);
                    if (changed || companyChanged) NotifyChanged();
                    return;
                }

                // Multiple branches exist
                if (preserveSelectionIfValid && !companyChanged)
                {
                    if (_currentContext.IsAllBranches)
                    {
                        return; // Keep All Branches
                    }

                    if (_currentContext.BranchId.HasValue)
                    {
                        var matching = activeBranches.FirstOrDefault(b => b.BranchId == _currentContext.BranchId.Value);
                        if (matching != null)
                        {
                            // Still valid active branch in this company
                            _currentContext.BranchCode = matching.BranchCode;
                            _currentContext.BranchName = matching.BranchName;
                            return;
                        }
                    }
                }

                // Default to first active branch
                var first = activeBranches[0];
                _currentContext.SetBranch(companyId, first.BranchId, first.BranchCode, first.BranchName);
                NotifyChanged();
            }
        }

        public bool SetActiveBranch(int companyId, int? branchId, IEnumerable<Branch>? companyBranches)
        {
            lock (_lock)
            {
                if (companyId <= 0)
                {
                    Clear();
                    return false;
                }

                _currentContext.CompanyId = companyId;

                if (!branchId.HasValue || branchId.Value <= 0)
                {
                    _currentContext.SetAllBranches(companyId);
                    NotifyChanged();
                    return true;
                }

                var branch = (companyBranches ?? Enumerable.Empty<Branch>())
                    .FirstOrDefault(b => b.CompanyId == companyId && b.BranchId == branchId.Value && b.IsActive);

                if (branch == null)
                {
                    // Invalid or cross-company branch attempt rejected
                    return false;
                }

                _currentContext.SetBranch(companyId, branch.BranchId, branch.BranchCode, branch.BranchName);
                NotifyChanged();
                return true;
            }
        }

        public void SetAllBranches(int companyId)
        {
            lock (_lock)
            {
                _currentContext.SetAllBranches(companyId);
                NotifyChanged();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _currentContext.Clear();
                NotifyChanged();
            }
        }
    }
}
