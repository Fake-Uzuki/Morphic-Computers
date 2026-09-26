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

        private void NotifyChanged(ActiveBranchContext snapshot)
        {
            ActiveBranchChanged?.Invoke();
            ContextChanged?.Invoke(snapshot);
        }

        public ActiveBranchContext CurrentContext
        {
            get
            {
                lock (_lock)
                {
                    return CloneCurrentContext();
                }
            }
        }

        private ActiveBranchContext CloneCurrentContext()
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

        public int CurrentCompanyId
        {
            get { lock (_lock) return _currentContext.CompanyId; }
        }

        public int? CurrentBranchId
        {
            get { lock (_lock) return _currentContext.BranchId; }
        }

        public string? CurrentBranchCode
        {
            get { lock (_lock) return _currentContext.BranchCode; }
        }

        public string? CurrentBranchName
        {
            get { lock (_lock) return _currentContext.BranchName; }
        }

        public bool IsAllBranches
        {
            get { lock (_lock) return _currentContext.IsAllBranches; }
        }

        public bool HasSpecificBranch
        {
            get { lock (_lock) return _currentContext.HasSpecificBranch; }
        }

        /// <summary>
        /// Synchronizes and updates the branch context for a tenant's branches.
        /// If only 1 active branch exists, it is automatically selected.
        /// If the currently selected branch was deactivated, it falls back or clears.
        /// Dispatches notifications outside the lock to prevent deadlocks with UI threads.
        /// </summary>
        public void SyncBranches(int companyId, IEnumerable<Branch>? branches, bool preserveSelectionIfValid = true)
        {
            bool shouldNotify = false;
            ActiveBranchContext? snapshot = null;

            lock (_lock)
            {
                if (companyId <= 0)
                {
                    if (_currentContext.CompanyId != 0 || _currentContext.BranchId.HasValue || _currentContext.IsAllBranches)
                    {
                        _currentContext.Clear();
                        shouldNotify = true;
                        snapshot = CloneCurrentContext();
                    }
                }
                else
                {
                    bool companyChanged = _currentContext.CompanyId != companyId;
                    if (companyChanged)
                    {
                        _currentContext.Clear();
                        _currentContext.CompanyId = companyId;
                        shouldNotify = true;
                    }

                    var activeBranches = (branches ?? Enumerable.Empty<Branch>())
                        .Where(b => b.CompanyId == companyId && b.IsActive)
                        .ToList();

                    if (activeBranches.Count == 0)
                    {
                        if (_currentContext.BranchId.HasValue || _currentContext.IsAllBranches)
                        {
                            _currentContext.Clear();
                            _currentContext.CompanyId = companyId;
                            shouldNotify = true;
                        }
                    }
                    else if (activeBranches.Count == 1)
                    {
                        var single = activeBranches[0];
                        bool changed = _currentContext.BranchId != single.BranchId || _currentContext.IsAllBranches;
                        if (changed || companyChanged)
                        {
                            _currentContext.SetBranch(companyId, single.BranchId, single.BranchCode, single.BranchName);
                            shouldNotify = true;
                        }
                    }
                    else
                    {
                        // Multiple branches exist
                        bool selectionValid = false;
                        if (preserveSelectionIfValid && !companyChanged)
                        {
                            if (_currentContext.IsAllBranches)
                            {
                                selectionValid = true; // Keep All Branches
                            }
                            else if (_currentContext.BranchId.HasValue)
                            {
                                var matching = activeBranches.FirstOrDefault(b => b.BranchId == _currentContext.BranchId.Value);
                                if (matching != null)
                                {
                                    // Still valid active branch in this company
                                    _currentContext.BranchCode = matching.BranchCode;
                                    _currentContext.BranchName = matching.BranchName;
                                    selectionValid = true;
                                }
                            }
                        }

                        if (!selectionValid)
                        {
                            // Default to first active branch
                            var first = activeBranches[0];
                            bool changed = _currentContext.BranchId != first.BranchId || _currentContext.IsAllBranches;
                            if (changed || companyChanged)
                            {
                                _currentContext.SetBranch(companyId, first.BranchId, first.BranchCode, first.BranchName);
                                shouldNotify = true;
                            }
                        }
                    }

                    if (shouldNotify)
                    {
                        snapshot = CloneCurrentContext();
                    }
                }
            }

            if (shouldNotify && snapshot != null)
            {
                NotifyChanged(snapshot);
            }
        }

        public bool SetActiveBranch(int companyId, int? branchId, IEnumerable<Branch>? companyBranches)
        {
            bool shouldNotify = false;
            ActiveBranchContext? snapshot = null;

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
                    if (!_currentContext.IsAllBranches)
                    {
                        _currentContext.SetAllBranches(companyId);
                        shouldNotify = true;
                        snapshot = CloneCurrentContext();
                    }
                }
                else
                {
                    var branch = (companyBranches ?? Enumerable.Empty<Branch>())
                        .FirstOrDefault(b => b.CompanyId == companyId && b.BranchId == branchId.Value && b.IsActive);

                    if (branch == null)
                    {
                        // Invalid or cross-company branch attempt rejected
                        return false;
                    }

                    if (_currentContext.BranchId != branch.BranchId || _currentContext.IsAllBranches)
                    {
                        _currentContext.SetBranch(companyId, branch.BranchId, branch.BranchCode, branch.BranchName);
                        shouldNotify = true;
                        snapshot = CloneCurrentContext();
                    }
                }
            }

            if (shouldNotify && snapshot != null)
            {
                NotifyChanged(snapshot);
            }

            return true;
        }

        public void SetAllBranches(int companyId)
        {
            bool shouldNotify = false;
            ActiveBranchContext? snapshot = null;

            lock (_lock)
            {
                if (_currentContext.CompanyId != companyId || !_currentContext.IsAllBranches)
                {
                    _currentContext.SetAllBranches(companyId);
                    shouldNotify = true;
                    snapshot = CloneCurrentContext();
                }
            }

            if (shouldNotify && snapshot != null)
            {
                NotifyChanged(snapshot);
            }
        }

        public void Clear()
        {
            bool shouldNotify = false;
            ActiveBranchContext? snapshot = null;

            lock (_lock)
            {
                if (_currentContext.CompanyId != 0 || _currentContext.BranchId.HasValue || _currentContext.IsAllBranches)
                {
                    _currentContext.Clear();
                    shouldNotify = true;
                    snapshot = CloneCurrentContext();
                }
            }

            if (shouldNotify && snapshot != null)
            {
                NotifyChanged(snapshot);
            }
        }
    }
}
