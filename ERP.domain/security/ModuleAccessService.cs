using System;
using System.Collections.Generic;

namespace ERP.domain.security
{
    /// <summary>
    /// Centralized module access and feature entitlement engine.
    /// Provides consistent, plan-based module authorization across ERP.api and ERP.winforms.
    /// </summary>
    public static class ModuleAccessService
    {
        /// <summary>
        /// Normalizes a plan name string into an ErpPlan enum.
        /// Handles legacy aliases like "SmallBusiness", "Platform", "Master".
        /// </summary>
        public static ErpPlan NormalizePlan(string? planName)
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                return ErpPlan.Micro;
            }

            string clean = planName.Trim();

            if (clean.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Super Administrator", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Platform", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Master", StringComparison.OrdinalIgnoreCase))
            {
                return ErpPlan.SuperAdmin;
            }

            if (clean.Equals("Medium", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("MediumEnterprise", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Enterprise", StringComparison.OrdinalIgnoreCase))
            {
                return ErpPlan.Medium;
            }

            if (clean.Equals("Small", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("SmallBusiness", StringComparison.OrdinalIgnoreCase))
            {
                return ErpPlan.Small;
            }

            return ErpPlan.Micro;
        }

        /// <summary>
        /// Maps common module string names/aliases to ErpModule enum.
        /// </summary>
        public static bool TryParseModule(string moduleName, out ErpModule module)
        {
            module = ErpModule.POS;
            if (string.IsNullOrWhiteSpace(moduleName)) return false;

            string clean = moduleName.Trim();

            if (Enum.TryParse(clean, true, out ErpModule parsed))
            {
                module = parsed;
                return true;
            }

            // Aliases
            if (clean.Equals("PointOfSale", StringComparison.OrdinalIgnoreCase) || clean.Equals("Sales", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.POS;
                return true;
            }
            if (clean.Equals("Stock", StringComparison.OrdinalIgnoreCase) || clean.Equals("ProductsStock", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.Products;
                return true;
            }
            if (clean.Equals("Policies", StringComparison.OrdinalIgnoreCase) || clean.Equals("Terms", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.StorePolicies;
                return true;
            }
            if (clean.Equals("BI", StringComparison.OrdinalIgnoreCase) || clean.Equals("Analytics", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.BusinessIntelligence;
                return true;
            }
            if (clean.Equals("Branches", StringComparison.OrdinalIgnoreCase) || clean.Equals("Branch", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.BranchManagement;
                return true;
            }
            if (clean.Equals("SupplyChain", StringComparison.OrdinalIgnoreCase) || clean.Equals("PurchaseOrders", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.Procurement;
                return true;
            }
            if (clean.Equals("Finance", StringComparison.OrdinalIgnoreCase) || clean.Equals("P&L", StringComparison.OrdinalIgnoreCase) || clean.Equals("Statements", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.FinancialStatements;
                return true;
            }
            if (clean.Equals("PlatformBI", StringComparison.OrdinalIgnoreCase))
            {
                module = ErpModule.PlatformBusinessIntelligence;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Answers whether a specific module is enabled for a given plan name string.
        /// </summary>
        public static bool IsModuleEnabled(string? planName, string moduleName)
        {
            var plan = NormalizePlan(planName);
            if (TryParseModule(moduleName, out ErpModule module))
            {
                return IsModuleEnabled(plan, module);
            }

            return false;
        }

        /// <summary>
        /// Answers whether a specific module is enabled for a given plan name string and ErpModule.
        /// </summary>
        public static bool IsModuleEnabled(string? planName, ErpModule module)
        {
            return IsModuleEnabled(NormalizePlan(planName), module);
        }

        /// <summary>
        /// Answers whether a specific module is enabled for a given ErpPlan.
        /// This is the single source of truth for all plan-based module access decisions
        /// across both ERP.api and ERP.winforms.
        /// </summary>
        public static bool IsModuleEnabled(ErpPlan plan, ErpModule module)
        {
            switch (plan)
            {
                // --------------------------------------------------------
                // MICRO — Tenant A
                // Core POS/Inventory/Sales operations only.
                // Keeps Main Generative Income.
                // No staff team, no customers, no suppliers, no repairs,
                // no policies, no payroll, no finance, no procurement,
                // no branch management.
                // --------------------------------------------------------
                case ErpPlan.Micro:
                    return module switch
                    {
                        ErpModule.Dashboard             => true,
                        ErpModule.POS                   => true,
                        ErpModule.Inventory             => true,
                        ErpModule.Products              => true,
                        ErpModule.Orders                => true,
                        ErpModule.Reports               => true,
                        ErpModule.MainGenerativeIncome  => true,

                        // Restricted for Micro
                        ErpModule.Repairs               => false,
                        ErpModule.Customers             => false,
                        ErpModule.Suppliers             => false,
                        ErpModule.Staff                 => false,
                        ErpModule.StorePolicies         => false,
                        ErpModule.Approvals             => false,
                        ErpModule.SupportGenerativeIncome => false,
                        ErpModule.BusinessIntelligence  => false,
                        ErpModule.BranchManagement      => false,
                        ErpModule.Procurement           => false,
                        ErpModule.Payroll               => false,
                        ErpModule.FinancialStatements   => false,

                        _ => false
                    };

                // --------------------------------------------------------
                // SMALL — Tenant B
                // Full operational modules. Adds Payroll, Finance, and
                // Supply Chain / Procurement vs Micro.
                // Keeps existing Repair Services module; removes Main Generative Income.
                // No Branch Management (single-location company).
                // --------------------------------------------------------
                case ErpPlan.Small:
                    return module switch
                    {
                        ErpModule.Dashboard             => true,
                        ErpModule.POS                   => true,
                        ErpModule.Inventory             => true,
                        ErpModule.Products              => true,
                        ErpModule.Orders                => true,
                        ErpModule.Repairs               => true,
                        ErpModule.Customers             => true,
                        ErpModule.Suppliers             => true,
                        ErpModule.Staff                 => true,
                        ErpModule.StorePolicies         => true,
                        ErpModule.Approvals             => true,
                        ErpModule.Payroll               => true,
                        ErpModule.FinancialStatements   => true,
                        ErpModule.Procurement           => true,
                        ErpModule.Reports               => true,

                        // Restricted for Small
                        ErpModule.MainGenerativeIncome  => false,
                        ErpModule.SupportGenerativeIncome => false,
                        ErpModule.BusinessIntelligence  => false,
                        ErpModule.BranchManagement      => false,

                        _ => false
                    };

                // --------------------------------------------------------
                // MEDIUM — Tenant C
                // All Small modules plus Branch Management.
                // Keeps existing Repair Services module; removes Main Generative Income.
                // --------------------------------------------------------
                case ErpPlan.Medium:
                    return module switch
                    {
                        ErpModule.Dashboard             => true,
                        ErpModule.POS                   => true,
                        ErpModule.Inventory             => true,
                        ErpModule.Products              => true,
                        ErpModule.Orders                => true,
                        ErpModule.Repairs               => true,
                        ErpModule.Customers             => true,
                        ErpModule.Suppliers             => true,
                        ErpModule.Staff                 => true,
                        ErpModule.StorePolicies         => true,
                        ErpModule.Approvals             => true,
                        ErpModule.Payroll               => true,
                        ErpModule.FinancialStatements   => true,
                        ErpModule.Procurement           => true,
                        ErpModule.BranchManagement      => true,
                        ErpModule.Reports               => true,

                        // Restricted for Medium tenants
                        ErpModule.MainGenerativeIncome  => false,
                        ErpModule.SupportGenerativeIncome => false,
                        ErpModule.BusinessIntelligence  => false,

                        _ => false
                    };

                // --------------------------------------------------------
                // SUPER ADMIN — Platform / Master context only.
                // Strictly platform management modules.
                // No tenant operational modules are exposed.
                // --------------------------------------------------------
                case ErpPlan.SuperAdmin:
                    return module switch
                    {
                        ErpModule.AdminPanel                  => true,
                        ErpModule.TenantManagement            => true,
                        ErpModule.SubscriptionManagement      => true,
                        ErpModule.PlatformBusinessIntelligence => true,
                        ErpModule.BusinessIntelligence        => true,

                        // Super Admin MUST NOT perform tenant operational transactions
                        _ => false
                    };

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns whether this plan is permitted to perform normal tenant operational transactions
        /// (POS, inventory, repairs, checkout). Super Admin is strictly prohibited.
        /// </summary>
        public static bool IsTenantOperationalAllowed(ErpPlan plan)
        {
            return plan != ErpPlan.SuperAdmin;
        }

        /// <summary>
        /// Returns whether this plan is permitted to perform normal tenant operational transactions
        /// (POS, inventory, repairs, checkout) by plan name.
        /// </summary>
        public static bool IsTenantOperationalAllowed(string? planName)
        {
            return IsTenantOperationalAllowed(NormalizePlan(planName));
        }

        /// <summary>
        /// Gets all modules enabled for the specified plan.
        /// </summary>
        public static HashSet<ErpModule> GetEnabledModules(ErpPlan plan)
        {
            var enabled = new HashSet<ErpModule>();
            foreach (ErpModule m in Enum.GetValues(typeof(ErpModule)))
            {
                if (IsModuleEnabled(plan, m))
                {
                    enabled.Add(m);
                }
            }
            return enabled;
        }
    }
}
