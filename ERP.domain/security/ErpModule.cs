namespace ERP.domain.security
{
    public enum ErpModule
    {
        // Core Operational Modules (Required for TechStore operational use cases)
        POS,
        Inventory,
        Products,
        Orders,
        Repairs,
        Customers,
        Suppliers,
        Staff,
        StorePolicies,
        Approvals,

        // Generative Income Modules
        MainGenerativeIncome,
        SupportGenerativeIncome,

        // Reporting & Analytics
        Reports,
        BusinessIntelligence,

        // Medium Enterprise Modules
        BranchManagement,
        Procurement,
        Payroll,
        FinancialStatements,
        Dashboard,

        // Super Admin Platform Modules
        AdminPanel,
        TenantManagement,
        SubscriptionManagement,
        PlatformBusinessIntelligence
    }
}
