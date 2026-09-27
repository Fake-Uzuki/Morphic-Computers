using System;
using System.Collections.Generic;

namespace ERP.domain.security
{
    /// <summary>
    /// Centralized Role-Based Access Control (RBAC) engine.
    /// Determines whether a user's role is authorized to view or access a given ERP module,
    /// scoped within the company's subscription plan entitlements.
    /// </summary>
    public static class RoleAccessService
    {
        /// <summary>
        /// Evaluates whether a role by name is allowed to access a specific module in principle.
        /// </summary>
        public static bool IsRoleAllowed(string? role, ErpModule module)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            string cleanRole = role.Trim();

            // 1. Platform Super Administrator (Platform / Master context only)
            if (cleanRole.Contains("Super", StringComparison.OrdinalIgnoreCase))
            {
                return module switch
                {
                    ErpModule.AdminPanel                   => true,
                    ErpModule.TenantManagement             => true,
                    ErpModule.SubscriptionManagement       => true,
                    ErpModule.PlatformBusinessIntelligence => true,
                    ErpModule.BusinessIntelligence         => true,
                    _                                      => false
                };
            }

            // 2. Store Administrator / Owner — full access to all operational modules
            if (cleanRole.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                cleanRole.Contains("Owner", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 3. Store Manager — operational leadership over store modules
            if (cleanRole.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return module switch
                {
                    ErpModule.Dashboard            => true,
                    ErpModule.POS                  => true,
                    ErpModule.Inventory            => true,
                    ErpModule.Products             => true,
                    ErpModule.Orders               => true,
                    ErpModule.Repairs              => true,
                    ErpModule.Customers            => true,
                    ErpModule.Suppliers            => true,
                    ErpModule.Staff                => true,
                    ErpModule.StorePolicies        => true,
                    ErpModule.Approvals            => true,
                    ErpModule.Payroll              => true,
                    ErpModule.FinancialStatements  => true,
                    ErpModule.Procurement          => true,
                    ErpModule.BranchManagement     => true,
                    ErpModule.MainGenerativeIncome => true,
                    ErpModule.Reports              => true,
                    _                              => false
                };
            }

            // 4. Cashier Operations — front-of-house register, sales, customer lookup, and shift reports
            if (cleanRole.Contains("Cashier", StringComparison.OrdinalIgnoreCase))
            {
                return module switch
                {
                    ErpModule.Dashboard => true,
                    ErpModule.POS       => true,
                    ErpModule.Inventory => true,
                    ErpModule.Products  => true,
                    ErpModule.Orders    => true,
                    ErpModule.Customers => true,
                    ErpModule.Reports   => true,
                    _                   => false
                };
            }

            // 5. Hardware Technician — repair workbench, hardware parts, service tickets, customer repair contacts
            if (cleanRole.Contains("Tech", StringComparison.OrdinalIgnoreCase))
            {
                return module switch
                {
                    ErpModule.Dashboard            => true,
                    ErpModule.Repairs              => true,
                    ErpModule.Inventory            => true,
                    ErpModule.Products             => true,
                    ErpModule.Orders               => true,
                    ErpModule.Customers            => true,
                    ErpModule.MainGenerativeIncome => true,
                    _                              => false
                };
            }

            // Fallback for custom or unrecognized roles: view dashboard and products only
            return module == ErpModule.Dashboard || module == ErpModule.Products;
        }

        /// <summary>
        /// Answers whether a user with a given role has effective access to a module
        /// considering BOTH the company's plan entitlement AND their RBAC role permission.
        /// </summary>
        public static bool HasAccess(ErpPlan plan, string? role, ErpModule module)
        {
            // Plan entitlement is the primary outer boundary: if plan doesn't enable it, no role can access it.
            if (!ModuleAccessService.IsModuleEnabled(plan, module))
            {
                return false;
            }

            // Role authorization is the inner boundary: user must have the required role.
            return IsRoleAllowed(role, module);
        }

        /// <summary>
        /// Answers whether a user with a given role has effective access to a module by plan name string.
        /// </summary>
        public static bool HasAccess(string? planName, string? role, ErpModule module)
        {
            var plan = ModuleAccessService.NormalizePlan(planName);
            return HasAccess(plan, role, module);
        }

        /// <summary>
        /// Returns all effective modules allowed for a role under a given plan.
        /// </summary>
        public static HashSet<ErpModule> GetAllowedModules(ErpPlan plan, string? role)
        {
            var allowed = new HashSet<ErpModule>();
            foreach (ErpModule m in Enum.GetValues(typeof(ErpModule)))
            {
                if (HasAccess(plan, role, m))
                {
                    allowed.Add(m);
                }
            }
            return allowed;
        }
    }
}
