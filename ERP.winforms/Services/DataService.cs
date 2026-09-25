using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Data Access Service connecting ERP.winforms UI to ERP.api over HTTPS.
    /// Communicates with backend REST API using internal security authentication without exposing raw database credentials.
    /// </summary>
    public partial class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        private readonly ApiClient _apiClient = ApiClient.Instance;
        private readonly LocalDatabaseService _localDb = LocalDatabaseService.Instance;

        public List<Company> Companies { get; private set; } = new();
        public List<Product> Products { get; private set; } = new();
        public List<Category> Categories { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();
        public List<RepairTicket> RepairTickets { get; private set; } = new();
        public List<Supplier> Suppliers { get; private set; } = new();
        public List<StaffMember> StaffMembers { get; private set; } = new();
        public List<ApprovalRequest> ApprovalRequests { get; private set; } = new();
        public List<Customer> Customers { get; private set; } = new();
        public List<PayrollRecord> PayrollRecords { get; private set; } = new();
        public List<StorePolicy> StorePolicies { get; private set; } = new();
        public List<ExpenseRecord> ExpenseRecords { get; private set; } = new();

        public Action? CategoriesChanged;
        public Action? ProductsChanged;
        public Action? RepairTicketsChanged;
        public Action? SuppliersChanged;
        public Action? StaffMembersChanged;
        public Action? ApprovalRequestsChanged;
        public Action? CustomersChanged;
        public Action? PayrollRecordsChanged;
        public Action? StorePoliciesChanged;
        public Action? ExpensesChanged;
        public Action? OrdersChanged;
        public Action<bool>? ConnectionStatusChanged;

        private int _activeCompanyId = 1;
        public int ActiveCompanyId
        {
            get => _activeCompanyId;
            set
            {
                if (_activeCompanyId != value)
                {
                    _activeCompanyId = value;
                    SwitchActiveTenantData();
                }
            }
        }
        public bool IsUsingLiveCloudDatabase { get; set; }

        public Company? ActiveCompany => Companies.FirstOrDefault(c => c.CompanyId == ActiveCompanyId);

        public Company CurrentCompany
        {
            get => ActiveCompany ?? (Companies.Count > 0 ? Companies[0] : new Company { CompanyId = 1, CompanyName = "Tenant A", PlanName = "Micro" });
            set
            {
                if (value != null)
                {
                    ActiveCompanyId = value.CompanyId;
                }
            }
        }

        public void SwitchActiveTenantData()
        {
            Categories.Clear();
            Products.Clear();
            Orders.Clear();
            RepairTickets.Clear();
            Suppliers.Clear();
            StaffMembers.Clear();
            ApprovalRequests.Clear();
            Customers.Clear();
            PayrollRecords.Clear();
            StorePolicies.Clear();
            ExpenseRecords.Clear();

            if (ActiveCompanyId <= 0 || string.Equals(CurrentCompany?.PlanName, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                CategoriesChanged?.Invoke();
                ProductsChanged?.Invoke();
                RepairTicketsChanged?.Invoke();
                SuppliersChanged?.Invoke();
                StaffMembersChanged?.Invoke();
                ApprovalRequestsChanged?.Invoke();
                CustomersChanged?.Invoke();
                PayrollRecordsChanged?.Invoke();
                StorePoliciesChanged?.Invoke();
                ExpensesChanged?.Invoke();
                return;
            }

            try
            {
                Categories = Task.Run(() => _localDb.GetCategoriesAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Products = Task.Run(() => _localDb.GetProductsAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Orders = Task.Run(() => _localDb.GetOrdersAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                RepairTickets = Task.Run(() => _localDb.GetRepairsAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Suppliers = Task.Run(() => _localDb.GetSuppliersAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                StaffMembers = Task.Run(() => _localDb.GetStaffAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                ApprovalRequests = Task.Run(() => _localDb.GetApprovalsAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Customers = Task.Run(() => _localDb.GetCustomersAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                PayrollRecords = Task.Run(() => _localDb.GetPayrollAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                StorePolicies = Task.Run(() => _localDb.GetPoliciesAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                ExpenseRecords = Task.Run(() => _localDb.GetExpensesAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                if (ExpenseRecords.Count == 0)
                {
                    MigrateJsonExpensesIfAvailable(ActiveCompanyId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Local SQL tenant switch note: {ex.Message}");
            }

            CategoriesChanged?.Invoke();
            ProductsChanged?.Invoke();
            RepairTicketsChanged?.Invoke();
            SuppliersChanged?.Invoke();
            StaffMembersChanged?.Invoke();
            ApprovalRequestsChanged?.Invoke();
            CustomersChanged?.Invoke();
            PayrollRecordsChanged?.Invoke();
            StorePoliciesChanged?.Invoke();
            ExpensesChanged?.Invoke();

            Task.Run(() => LoadFromDatabase());
        }

        private DataService()
        {
            InitializeDataStore();
        }

        private void InitializeDataStore()
        {
            // Registered companies with Tenant C and dynamic master resolution
            Companies = new List<Company>
            {
                new Company { CompanyId = 1, CompanyCode = "TENANT_A", CompanyName = "Tenant A", PlanName = "Micro", Description = "Micro Store Operations" },
                new Company { CompanyId = 2, CompanyCode = "TENANT_B", CompanyName = "Tenant B", PlanName = "Small", Description = "Small Business Store Operations" },
                new Company { CompanyId = 1001, CompanyCode = "TENANT_C", CompanyName = "Tenant C", PlanName = "Medium", Description = "Medium Enterprise Store Operations" }
            };

            try
            {
                using var masterDb = LocalTenantDbContextProvider.CreateMasterDbContext();
                var masterCompanies = masterDb.Companies.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CompanyId).ToList();
                if (masterCompanies.Count > 0)
                {
                    Companies = masterCompanies;
                }
            }
            catch { }

            Categories = new List<Category>();

            // Immediate local SQL database hydration: UI is instantly populated from local tenant SQL DB
            try
            {
                Categories = Task.Run(() => _localDb.GetCategoriesAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Products = Task.Run(() => _localDb.GetProductsAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Orders = Task.Run(() => _localDb.GetOrdersAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                RepairTickets = Task.Run(() => _localDb.GetRepairsAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Suppliers = Task.Run(() => _localDb.GetSuppliersAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                StaffMembers = Task.Run(() => _localDb.GetStaffAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                ApprovalRequests = Task.Run(() => _localDb.GetApprovalsAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                Customers = Task.Run(() => _localDb.GetCustomersAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                PayrollRecords = Task.Run(() => _localDb.GetPayrollAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                StorePolicies = Task.Run(() => _localDb.GetPoliciesAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                ExpenseRecords = Task.Run(() => _localDb.GetExpensesAsync(ActiveCompanyId)).GetAwaiter().GetResult() ?? new();
                if (ExpenseRecords.Count == 0)
                {
                    MigrateJsonExpensesIfAvailable(ActiveCompanyId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Local SQL initialization note: {ex.Message}");
            }

            // Background live cloud refresh (or local database fallback when offline)
            Task.Run(() => LoadFromDatabase());
        }

        private static void NormalizeProductCategories(IEnumerable<Product> products)
        {
            foreach (var p in products)
            {
                if (string.IsNullOrWhiteSpace(p.CategoryName) || p.CategoryName.Equals("General", StringComparison.OrdinalIgnoreCase))
                {
                    string nameLower = (p.ProductName + " " + p.ProductCode).ToLowerInvariant();
                    if (nameLower.Contains("cpu") || nameLower.Contains("ryzen") || nameLower.Contains("intel") || nameLower.Contains("processor"))
                        p.CategoryName = "Processors (CPU)";
                    else if (nameLower.Contains("ram") || nameLower.Contains("ddr") || nameLower.Contains("memory"))
                        p.CategoryName = "Memory (RAM)";
                    else if (nameLower.Contains("ssd") || nameLower.Contains("hdd") || nameLower.Contains("storage") || nameLower.Contains("nvme"))
                        p.CategoryName = "Storage (SSD/HDD)";
                    else if (nameLower.Contains("mouse") || nameLower.Contains("keyboard") || nameLower.Contains("headset") || nameLower.Contains("peripheral"))
                        p.CategoryName = "Peripherals";
                    else if (nameLower.Contains("motherboard") || nameLower.Contains("board") || nameLower.Contains("b550") || nameLower.Contains("x570") || nameLower.Contains("z790"))
                        p.CategoryName = "Motherboards";
                    else
                        p.CategoryName = "Graphics Cards (GPU)";
                }
            }
        }

        public async Task<List<Inventory>> GetInventoriesAsync(int? companyId = null)
        {
            int cid = companyId ?? ActiveCompanyId;
            return await _localDb.GetInventoriesAsync(cid).ConfigureAwait(false);
        }

        public void LoadFromDatabase()
        {
            if (ActiveCompanyId <= 0 || string.Equals(CurrentCompany?.PlanName, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int targetCompanyId = ActiveCompanyId;
            bool networkUp = NetworkInterface.GetIsNetworkAvailable();
            bool anyApiSucceeded = false;

            // 1. Fetch live categories for the active tenant via API (fallback to local DB)
            try
            {
                List<Category>? liveCategories = null;
                if (networkUp)
                {
                    try
                    {
                        liveCategories = Task.Run(() => _apiClient.GetCategoriesAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetCategories note: {ex.Message}");
                        liveCategories = null;
                    }
                }

                if (liveCategories != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        Categories = liveCategories;
                        SaveCategoriesToLocalCache();
                        CategoriesChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localCategories = Task.Run(() => _localDb.GetCategoriesAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localCategories != null && ActiveCompanyId == targetCompanyId)
                        {
                            Categories = localCategories;
                            SaveCategoriesToLocalCache();
                            CategoriesChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetCategories fallback error: {dbEx.Message}");
                        if (Categories.Count == 0 && ActiveCompanyId == targetCompanyId) LoadCategoriesFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Categories loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 2. Fetch live products for the active tenant via API (fallback to local DB)
            try
            {
                List<Product>? liveProducts = null;
                if (networkUp)
                {
                    try
                    {
                        liveProducts = Task.Run(() => _apiClient.GetProductsAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetProducts note: {ex.Message}");
                        liveProducts = null;
                    }
                }

                if (liveProducts != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        Products = liveProducts
                            .GroupBy(p => p.ProductCode)
                            .Select(g => g.First())
                            .ToList();
                        NormalizeProductCategories(Products);
                        SaveProductsToLocalCache();
                        ProductsChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localProducts = Task.Run(() => _localDb.GetProductsAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localProducts != null && ActiveCompanyId == targetCompanyId)
                        {
                            Products = localProducts
                                .GroupBy(p => p.ProductCode)
                                .Select(g => g.First())
                                .ToList();
                            NormalizeProductCategories(Products);
                            SaveProductsToLocalCache();
                            ProductsChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetProducts fallback error: {dbEx.Message}");
                        if (Products.Count == 0 && ActiveCompanyId == targetCompanyId) LoadProductsToLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Products loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 3. Fetch live orders for the active tenant via API (fallback to local DB)
            try
            {
                List<Order>? liveOrders = null;
                if (networkUp)
                {
                    try
                    {
                        liveOrders = Task.Run(() => _apiClient.GetOrdersAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetOrders note: {ex.Message}");
                        liveOrders = null;
                    }
                }

                if (liveOrders != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        Orders = liveOrders
                            .GroupBy(o => o.Id)
                            .Select(g => g.First())
                            .OrderByDescending(o => o.CreatedAt)
                            .ToList();
                        SaveOrdersToLocalCache();
                        OrdersChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localOrders = Task.Run(() => _localDb.GetOrdersAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localOrders != null && ActiveCompanyId == targetCompanyId)
                        {
                            Orders = localOrders
                                .GroupBy(o => o.Id)
                                .Select(g => g.First())
                                .OrderByDescending(o => o.CreatedAt)
                                .ToList();
                            SaveOrdersToLocalCache();
                            OrdersChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetOrders fallback error: {dbEx.Message}");
                        if (Orders.Count == 0 && ActiveCompanyId == targetCompanyId) LoadOrdersFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Orders loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 4. Fetch live repairs for the active tenant via API (fallback to local DB)
            try
            {
                List<RepairTicket>? liveRepairs = null;
                if (networkUp)
                {
                    try
                    {
                        liveRepairs = Task.Run(() => _apiClient.GetRepairsAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetRepairs note: {ex.Message}");
                        liveRepairs = null;
                    }
                }

                if (liveRepairs != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        RepairTickets = liveRepairs;
                        SaveRepairsToLocalCache();
                        RepairTicketsChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localRepairs = Task.Run(() => _localDb.GetRepairsAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localRepairs != null && ActiveCompanyId == targetCompanyId)
                        {
                            RepairTickets = localRepairs;
                            SaveRepairsToLocalCache();
                            RepairTicketsChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetRepairs fallback error: {dbEx.Message}");
                        if (RepairTickets.Count == 0 && ActiveCompanyId == targetCompanyId) LoadRepairsFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Repairs loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 5. Fetch live suppliers for the active tenant via API (fallback to local DB)
            try
            {
                List<Supplier>? liveSuppliers = null;
                if (networkUp)
                {
                    try
                    {
                        liveSuppliers = Task.Run(() => _apiClient.GetSuppliersAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetSuppliers note: {ex.Message}");
                        liveSuppliers = null;
                    }
                }

                if (liveSuppliers != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        Suppliers = liveSuppliers;
                        SaveSuppliersToLocalCache();
                        SuppliersChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localSuppliers = Task.Run(() => _localDb.GetSuppliersAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localSuppliers != null && ActiveCompanyId == targetCompanyId)
                        {
                            Suppliers = localSuppliers;
                            SaveSuppliersToLocalCache();
                            SuppliersChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetSuppliers fallback error: {dbEx.Message}");
                        if (Suppliers.Count == 0 && ActiveCompanyId == targetCompanyId) LoadSuppliersFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Suppliers loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 6. Fetch live staff for the active tenant via API (fallback to local DB)
            try
            {
                List<StaffMember>? liveStaff = null;
                if (networkUp)
                {
                    try
                    {
                        liveStaff = Task.Run(() => _apiClient.GetStaffAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetStaff note: {ex.Message}");
                        liveStaff = null;
                    }
                }

                if (liveStaff != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        StaffMembers = liveStaff;
                        SaveStaffToLocalCache();
                        StaffMembersChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localStaff = Task.Run(() => _localDb.GetStaffAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localStaff != null && ActiveCompanyId == targetCompanyId)
                        {
                            StaffMembers = localStaff;
                            SaveStaffToLocalCache();
                            StaffMembersChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetStaff fallback error: {dbEx.Message}");
                        if (StaffMembers.Count == 0 && ActiveCompanyId == targetCompanyId) LoadStaffFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Staff loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 7. Fetch live approvals for the active tenant via API (fallback to local DB)
            try
            {
                List<ApprovalRequest>? liveApprovals = null;
                if (networkUp)
                {
                    try
                    {
                        liveApprovals = Task.Run(() => _apiClient.GetApprovalRequestsAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetApprovals note: {ex.Message}");
                        liveApprovals = null;
                    }
                }

                if (liveApprovals != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        ApprovalRequests = liveApprovals;
                        SaveApprovalsToLocalCache();
                        ApprovalRequestsChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localApprovals = Task.Run(() => _localDb.GetApprovalsAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localApprovals != null && ActiveCompanyId == targetCompanyId)
                        {
                            ApprovalRequests = localApprovals;
                            SaveApprovalsToLocalCache();
                            ApprovalRequestsChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetApprovals fallback error: {dbEx.Message}");
                        if (ApprovalRequests.Count == 0 && ActiveCompanyId == targetCompanyId) LoadApprovalsFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Approvals loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 8. Fetch live customers for the active tenant via API (fallback to local DB)
            try
            {
                List<Customer>? liveCustomers = null;
                if (networkUp)
                {
                    try
                    {
                        liveCustomers = Task.Run(() => _apiClient.GetCustomersAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetCustomers note: {ex.Message}");
                        liveCustomers = null;
                    }
                }

                if (liveCustomers != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        Customers = liveCustomers;
                        SaveCustomersToLocalCache();
                        CustomersChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localCustomers = Task.Run(() => _localDb.GetCustomersAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localCustomers != null && ActiveCompanyId == targetCompanyId)
                        {
                            Customers = localCustomers;
                            SaveCustomersToLocalCache();
                            CustomersChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetCustomers fallback error: {dbEx.Message}");
                        if (Customers.Count == 0 && ActiveCompanyId == targetCompanyId) LoadCustomersFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Customers loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 9. Fetch live payroll for the active tenant via API (fallback to local DB)
            try
            {
                List<PayrollRecord>? livePayroll = null;
                if (networkUp)
                {
                    try
                    {
                        livePayroll = Task.Run(() => _apiClient.GetPayrollAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetPayroll note: {ex.Message}");
                        livePayroll = null;
                    }
                }

                if (livePayroll != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        PayrollRecords = livePayroll;
                        SavePayrollToLocalCache();
                        PayrollRecordsChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localPayroll = Task.Run(() => _localDb.GetPayrollAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localPayroll != null && ActiveCompanyId == targetCompanyId)
                        {
                            PayrollRecords = localPayroll;
                            SavePayrollToLocalCache();
                            PayrollRecordsChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetPayroll fallback error: {dbEx.Message}");
                        if (PayrollRecords.Count == 0 && ActiveCompanyId == targetCompanyId) LoadPayrollFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Payroll loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 10. Fetch live policies for the active tenant via API (fallback to local DB)
            try
            {
                List<StorePolicy>? livePolicies = null;
                if (networkUp)
                {
                    try
                    {
                        livePolicies = Task.Run(() => _apiClient.GetPoliciesAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetPolicies note: {ex.Message}");
                        livePolicies = null;
                    }
                }

                if (livePolicies != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        StorePolicies = livePolicies;
                        SavePoliciesToLocalCache();
                        StorePoliciesChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localPolicies = Task.Run(() => _localDb.GetPoliciesAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localPolicies != null && ActiveCompanyId == targetCompanyId)
                        {
                            StorePolicies = localPolicies;
                            SavePoliciesToLocalCache();
                            StorePoliciesChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetPolicies fallback error: {dbEx.Message}");
                        if (StorePolicies.Count == 0 && ActiveCompanyId == targetCompanyId) LoadPoliciesFromLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Policies loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            // 11. Fetch live expenses for the active tenant via API (fallback to local DB)
            try
            {
                List<ExpenseRecord>? liveExpenses = null;
                if (networkUp)
                {
                    try
                    {
                        liveExpenses = Task.Run(() => _apiClient.GetExpensesAsync(targetCompanyId)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API GetExpenses note: {ex.Message}");
                        liveExpenses = null;
                    }
                }

                if (liveExpenses != null)
                {
                    anyApiSucceeded = true;
                    if (ActiveCompanyId == targetCompanyId)
                    {
                        ExpenseRecords = liveExpenses;
                        ExpensesChanged?.Invoke();
                    }
                }
                else
                {
                    try
                    {
                        var localExpenses = Task.Run(() => _localDb.GetExpensesAsync(targetCompanyId)).GetAwaiter().GetResult();
                        if (localExpenses != null && ActiveCompanyId == targetCompanyId)
                        {
                            ExpenseRecords = localExpenses;
                            ExpensesChanged?.Invoke();
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local DB GetExpenses fallback error: {dbEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService Expenses loading note: {ex.Message}");
            }

            if (ActiveCompanyId != targetCompanyId) return;

            IsUsingLiveCloudDatabase = anyApiSucceeded;
            ConnectionStatusChanged?.Invoke(anyApiSucceeded);
        }

        private string GetLocalOrdersFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_orders.json");
        }

        private void SaveOrdersToLocalCache()
        {
            try
            {
                string path = GetLocalOrdersFilePath();
                string json = JsonSerializer.Serialize(Orders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                OrdersChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveOrdersToLocalCache error: {ex.Message}");
            }
        }

        private void LoadOrdersFromLocalCache()
        {
            try
            {
                string path = GetLocalOrdersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Order>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Orders = cached
                            .GroupBy(o => o.Id)
                            .Select(g => g.First())
                            .OrderByDescending(o => o.CreatedAt)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadOrdersFromLocalCache error: {ex.Message}");
            }
        }

        private string GetLocalCategoriesFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_categories.json");
        }

        private void SaveCategoriesToLocalCache()
        {
            try
            {
                string path = GetLocalCategoriesFilePath();
                string json = JsonSerializer.Serialize(Categories, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCategoriesToLocalCache error: {ex.Message}");
            }
        }

        private void LoadCategoriesFromLocalCache()
        {
            try
            {
                string path = GetLocalCategoriesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Category>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Categories = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCategoriesFromLocalCache error: {ex.Message}");
            }
        }

        private string GetLocalProductsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_products.json");
        }

        private void SaveProductsToLocalCache()
        {
            try
            {
                string path = GetLocalProductsFilePath();
                string json = JsonSerializer.Serialize(Products, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveProductsToLocalCache error: {ex.Message}");
            }
        }

        private void LoadProductsToLocalCache()
        {
            try
            {
                string path = GetLocalProductsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Product>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        Products = cached
                            .GroupBy(p => p.ProductCode)
                            .Select(g => g.First())
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProductsToLocalCache error: {ex.Message}");
            }
        }



        public static readonly HashSet<string> DefaultPresetNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Graphics Cards (GPU)",
            "Processors (CPU)",
            "Memory (RAM)",
            "Storage (SSD/HDD)",
            "Peripherals"
        };

        public bool IsApiReachable()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) return false;
            try
            {
                return Task.Run(() => _apiClient.CheckApiConnectivityAsync()).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        public bool IsDefaultPreset(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return false;
            return DefaultPresetNames.Contains(categoryName.Trim());
        }

        public bool AddCategory(Category category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name)) return false;
            category.Name = category.Name.Trim();

            var existing = Categories.FirstOrDefault(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return true;

            category.CompanyId = ActiveCompanyId;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                Category? apiCreated = null;
                try
                {
                    apiCreated = Task.Run(() => _apiClient.AddCategoryAsync(ActiveCompanyId, category)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddCategory API error: {ex.Message}");
                }

                if (apiCreated == null)
                {
                    return false;
                }

                category.Id = apiCreated.Id;
                try
                {
                    Task.Run(() => _localDb.AddCategoryAsync(ActiveCompanyId, category, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var localCreated = Task.Run(() => _localDb.AddCategoryAsync(ActiveCompanyId, category, enqueueSync: true)).GetAwaiter().GetResult();
                    if (localCreated != null && localCreated.Id > 0)
                    {
                        category.Id = localCreated.Id;
                    }
                    else
                    {
                        int nextId = Categories.Count > 0 ? Categories.Max(c => c.Id) + 1 : 1;
                        category.Id = nextId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddCategory localDb error: {ex.Message}");
                    return false;
                }
            }

            if (!Categories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Categories.Add(category);
            }
            CategoriesChanged?.Invoke();
            return true;
        }

        public bool UpdateCategory(int categoryId, string newName, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(newName)) return false;
            newName = newName.Trim();

            var cat = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (cat == null) return false;

            // Check duplicate
            var dup = Categories.FirstOrDefault(c => c.Id != categoryId && c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (dup != null) return false;

            string oldName = cat.Name;
            var updatedCat = new Category
            {
                Id = categoryId,
                CompanyId = ActiveCompanyId,
                Name = newName,
                Description = description?.Trim() ?? cat.Description
            };

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    var res = Task.Run(() => _apiClient.UpdateCategoryAsync(ActiveCompanyId, categoryId, updatedCat)).GetAwaiter().GetResult();
                    apiSuccess = res != null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCategory API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateCategoryAsync(ActiveCompanyId, categoryId, newName, description, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateCategoryAsync(ActiveCompanyId, categoryId, newName, description, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCategory localDb error: {ex.Message}");
                    return false;
                }
            }

            cat.Name = newName;
            if (description != null) cat.Description = description.Trim();

            // Cascade update to in-memory products
            if (!oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var p in Products.Where(p => p.CategoryName.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                {
                    p.CategoryName = newName;
                }
            }

            CategoriesChanged?.Invoke();
            return true;
        }

        public bool DeleteCategory(int categoryId, string? reassignTo = null)
        {
            var cat = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (cat == null) return false;

            if (IsDefaultPreset(cat.Name)) return false; // Protected

            string fallback = string.IsNullOrWhiteSpace(reassignTo) ? "Graphics Cards (GPU)" : reassignTo.Trim();
            string oldName = cat.Name;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.DeleteCategoryAsync(ActiveCompanyId, categoryId, fallback)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteCategory API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.DeleteCategoryAsync(ActiveCompanyId, categoryId, fallback, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.DeleteCategoryAsync(ActiveCompanyId, categoryId, fallback, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteCategory localDb error: {ex.Message}");
                    return false;
                }
            }

            // Reassign in-memory products
            foreach (var p in Products.Where(p => p.CategoryName.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
            {
                p.CategoryName = fallback;
            }

            Categories.Remove(cat);
            CategoriesChanged?.Invoke();
            return true;
        }

        public bool AddProduct(Product product) => SaveProduct(product);

        public bool SaveProduct(Product product)
        {
            if (product == null) return false;

            try
            {
                // Check if already in memory
                if (product.ProductId > 0)
                {
                    var existing = Products.FirstOrDefault(p => p.ProductId == product.ProductId);
                    if (existing != null)
                    {
                        return UpdateProduct(product);
                    }
                }
                else
                {
                    // New product: reject if SKU already in use to prevent overwriting
                    if (Products.Any(p => p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveProduct: duplicate SKU '{product.ProductCode}' rejected.");
                        return false;
                    }
                }

                bool isOnline = IsApiReachable();
                if (isOnline)
                {
                    Product? apiCreated = null;
                    try
                    {
                        apiCreated = Task.Run(() => _apiClient.AddProductAsync(ActiveCompanyId, product)).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveProduct API error: {ex.Message}");
                    }

                    if (apiCreated == null)
                    {
                        return false;
                    }

                    product.ProductId = apiCreated.ProductId;
                    try
                    {
                        Task.Run(() => _localDb.SaveProductAsync(ActiveCompanyId, product, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        var saved = Task.Run(() => _localDb.SaveProductAsync(ActiveCompanyId, product, enqueueSync: true)).GetAwaiter().GetResult();
                        if (saved != null && saved.ProductId > 0)
                        {
                            product.ProductId = saved.ProductId;
                        }
                        else
                        {
                            int nextId = Products.Count > 0 ? Products.Max(p => p.ProductId) + 1 : 1;
                            product.ProductId = nextId;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveProduct localDb error: {ex.Message}");
                        return false;
                    }
                }

                if (!Products.Any(p => p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase)))
                {
                    Products.Add(product);
                }
                ProductsChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveProduct general error: {ex.Message}");
                return false;
            }
        }

        public bool UpdateProduct(Product product)
        {
            if (product == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdateProductAsync(ActiveCompanyId, product)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateProduct API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.SaveProductAsync(ActiveCompanyId, product, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateProduct localDb mirror error: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.SaveProductAsync(ActiveCompanyId, product, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateProduct localDb offline error: {ex.Message}");
                    return false;
                }
            }

            var existing = Products.FirstOrDefault(p =>
                (product.ProductId > 0 && p.ProductId == product.ProductId) ||
                p.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.ProductName = product.ProductName;
                existing.ProductCode = product.ProductCode;
                existing.UnitPrice = product.UnitPrice;
                existing.StockQuantity = product.StockQuantity;
                existing.CategoryName = product.CategoryName;
                existing.Description = product.Description;
                existing.IsActive = product.IsActive;
                existing.SupplierName = product.SupplierName;
                existing.SupplierId = product.SupplierId;
            }

            // Ensure deduplication in memory
            Products = Products
                .GroupBy(p => p.ProductCode)
                .Select(g => g.First())
                .ToList();

            ProductsChanged?.Invoke();
            return true;
        }

        public bool ArchiveProduct(int productId)
        {
            var p = Products.FirstOrDefault(x => x.ProductId == productId);
            if (p == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.ArchiveProductAsync(ActiveCompanyId, productId)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ArchiveProduct API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.ArchiveProductAsync(ActiveCompanyId, productId, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.ArchiveProductAsync(ActiveCompanyId, productId, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ArchiveProduct localDb error: {ex.Message}");
                    return false;
                }
            }

            p.IsActive = false;
            p.ArchivedAt = DateTime.UtcNow;
            ProductsChanged?.Invoke();
            return true;
        }

        public bool RestoreProduct(int productId)
        {
            var p = Products.FirstOrDefault(x => x.ProductId == productId);
            if (p == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.RestoreProductAsync(ActiveCompanyId, productId)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RestoreProduct API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.RestoreProductAsync(ActiveCompanyId, productId, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.RestoreProductAsync(ActiveCompanyId, productId, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RestoreProduct localDb error: {ex.Message}");
                    return false;
                }
            }

            p.IsActive = true;
            p.ArchivedAt = null;
            ProductsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Enterprise Soft Delete: Archives product instead of permanently deleting to protect order audit history.
        /// </summary>
        public bool DeleteProduct(int productId) => ArchiveProduct(productId);

        public bool ProcessOrder(Order order)
        {
            if (order == null || order.Items == null || order.Items.Count == 0) return false;

            // Pre-validation of stock
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod == null || !prod.IsActive) return false;
                if (prod.StockQuantity < item.Quantity || prod.StockQuantity <= 0) return false;
            }

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.ProcessOrderAsync(ActiveCompanyId, order)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ProcessOrder API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                // Mirror to local SQL without enqueuing sync
                try
                {
                    Task.Run(() => _localDb.ProcessOrderAsync(ActiveCompanyId, order, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ProcessOrder localDb mirror error: {ex.Message}");
                }
            }
            else
            {
                // Truly offline: save to local DB and enqueue sync
                try
                {
                    bool localSuccess = Task.Run(() => _localDb.ProcessOrderAsync(ActiveCompanyId, order, enqueueSync: true)).GetAwaiter().GetResult();
                    if (!localSuccess) return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ProcessOrder localDb error: {ex.Message}");
                    return false;
                }
            }

            // Deduct stock for purchased items locally in memory
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity = Math.Max(0, prod.StockQuantity - item.Quantity);
                }
            }

            if (!Orders.Any(o => o.Id == order.Id))
            {
                Orders.Insert(0, order);
            }

            ProductsChanged?.Invoke();
            return true;
        }

        public bool VoidOrder(string orderId)
        {
            string cleanId = orderId.TrimStart('#');
            var order = Orders.FirstOrDefault(o => o.Id.TrimStart('#').Equals(cleanId, StringComparison.OrdinalIgnoreCase));
            if (order == null) return false;

            order.Status = "Voided";
            order.ArchivedAt = DateTime.UtcNow;

            // Restock items in inventory
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity += item.Quantity;
                }
            }

            try
            {
                Task.Run(() => _localDb.VoidOrderAsync(ActiveCompanyId, cleanId)).GetAwaiter().GetResult();
            }
            catch { }

            SaveOrdersToLocalCache();
            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();
            return true;
        }

        public bool RestoreOrder(string orderId)
        {
            string cleanId = orderId.TrimStart('#');
            var order = Orders.FirstOrDefault(o => o.Id.TrimStart('#').Equals(cleanId, StringComparison.OrdinalIgnoreCase));
            if (order == null) return false;

            order.Status = "Completed";
            order.ArchivedAt = null;

            // Re-deduct stock
            foreach (var item in order.Items)
            {
                var prod = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity = Math.Max(0, prod.StockQuantity - item.Quantity);
                }
            }

            try
            {
                Task.Run(() => _localDb.RestoreOrderAsync(ActiveCompanyId, cleanId)).GetAwaiter().GetResult();
            }
            catch { }

            SaveOrdersToLocalCache();
            SaveProductsToLocalCache();
            ProductsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Flushes all in-memory products, categories, and orders to local disk files.
        /// Called automatically during form closing and application exit.
        /// </summary>
        public void SaveAllToDisk()
        {
            try
            {
                SaveProductsToLocalCache();
                SaveCategoriesToLocalCache();
                SaveOrdersToLocalCache();
                SaveRepairsToLocalCache();
                SaveSuppliersToLocalCache();
                SaveStaffToLocalCache();
                SaveApprovalsToLocalCache();
                SaveCustomersToLocalCache();
                SavePayrollToLocalCache();
                SavePoliciesToLocalCache();
                System.Diagnostics.Debug.WriteLine("DataService.SaveAllToDisk: Successfully persisted all data to disk.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService.SaveAllToDisk error: {ex.Message}");
            }
        }

        /// <summary>
        /// Isolated Tenant Backup: Exports all Tenant A store data (Products, Orders) into a portable JSON backup file.
        /// </summary>
        public string ExportTenantBackup(string destinationPath)
        {
            var backupData = new
            {
                BackupDate = DateTime.UtcNow,
                Tenant = CurrentCompany.CompanyName,
                TotalProducts = Products.Count,
                TotalOrders = Orders.Count,
                TotalRevenue = GetTotalRevenue(),
                Products = Products.Select(p => new
                {
                    p.ProductId,
                    p.ProductCode,
                    p.ProductName,
                    p.UnitPrice,
                    p.StockQuantity,
                    p.CategoryName,
                    p.IsActive
                }),
                Orders = Orders.Select(o => new
                {
                    o.Id,
                    o.CustomerName,
                    o.CreatedAt,
                    o.TotalAmount,
                    o.PaymentMethod,
                    ItemCount = o.Items.Count
                })
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(backupData, jsonOptions);
            File.WriteAllText(destinationPath, json);
            return destinationPath;
        }

        // =========================================================================
        // TENANT B: SERVICE & REPAIR MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalRepairsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_repairs.json");
        }

        public void SaveRepairsToLocalCache()
        {
            try
            {
                string path = GetLocalRepairsFilePath();
                string json = JsonSerializer.Serialize(RepairTickets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveRepairsToLocalCache error: {ex.Message}");
            }
        }

        public void LoadRepairsFromLocalCache()
        {
            try
            {
                string path = GetLocalRepairsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<RepairTicket>>(json);
                    if (cached != null)
                    {
                        RepairTickets = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRepairsFromLocalCache error: {ex.Message}");
            }
        }

        public bool AddRepairTicket(RepairTicket ticket)
        {
            if (ticket == null) return false;
            ticket.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                ticket.TicketNumber = $"REP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.IsActive = true;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.CreateRepairTicketAsync(ActiveCompanyId, ticket)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddRepairTicket API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.AddRepairTicketAsync(ActiveCompanyId, ticket, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var saved = Task.Run(() => _localDb.AddRepairTicketAsync(ActiveCompanyId, ticket, enqueueSync: true)).GetAwaiter().GetResult();
                    if (saved != null && saved.RepairTicketId > 0)
                    {
                        ticket.RepairTicketId = saved.RepairTicketId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddRepairTicket localDb error: {ex.Message}");
                    return false;
                }
            }

            if (ticket.RepairTicketId == 0)
            {
                ticket.RepairTicketId = (RepairTickets.Count > 0 ? RepairTickets.Max(t => t.RepairTicketId) : 0) + 1;
            }

            RepairTickets.Insert(0, ticket);
            SaveRepairsToLocalCache();
            RepairTicketsChanged?.Invoke();
            return true;
        }

        public bool UpdateRepairStatus(int ticketId, string status, string? notes = null, string? technician = null)
        {
            var ticket = RepairTickets.FirstOrDefault(t => t.RepairTicketId == ticketId);
            if (ticket == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdateRepairStatusAsync(ActiveCompanyId, ticketId, status, notes, technician)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateRepairStatus API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateRepairStatusAsync(ActiveCompanyId, ticketId, status, notes, technician, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateRepairStatusAsync(ActiveCompanyId, ticketId, status, notes, technician, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateRepairStatus localDb error: {ex.Message}");
                    return false;
                }
            }

            ticket.Status = status;
            if (!string.IsNullOrEmpty(notes)) ticket.DiagnosticNotes = notes;
            if (!string.IsNullOrEmpty(technician)) ticket.AssignedTechnician = technician;
            if (status == "Completed" || status == "ReadyForPickup")
            {
                ticket.CompletedAt = DateTime.UtcNow;
            }

            SaveRepairsToLocalCache();
            RepairTicketsChanged?.Invoke();
            return true;
        }

        public bool UpdateRepairBilling(int ticketId, decimal labor, decimal parts, decimal deposit)
        {
            var ticket = RepairTickets.FirstOrDefault(t => t.RepairTicketId == ticketId);
            if (ticket == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdateRepairBillingAsync(ActiveCompanyId, ticketId, labor, parts, deposit)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateRepairBilling API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateRepairBillingAsync(ActiveCompanyId, ticketId, labor, parts, deposit, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateRepairBillingAsync(ActiveCompanyId, ticketId, labor, parts, deposit, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateRepairBilling localDb error: {ex.Message}");
                    return false;
                }
            }

            ticket.LaborFee = labor;
            ticket.PartsCost = parts;
            ticket.DepositAmount = deposit;

            SaveRepairsToLocalCache();
            RepairTicketsChanged?.Invoke();
            return true;
        }

        // =========================================================================
        // TENANT B: SUPPLIER MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalSuppliersFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_suppliers.json");
        }

        public void SaveSuppliersToLocalCache()
        {
            try
            {
                string path = GetLocalSuppliersFilePath();
                string json = JsonSerializer.Serialize(Suppliers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSuppliersToLocalCache error: {ex.Message}");
            }
        }

        public void LoadSuppliersFromLocalCache()
        {
            try
            {
                string path = GetLocalSuppliersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Supplier>>(json);
                    if (cached != null)
                    {
                        Suppliers = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSuppliersFromLocalCache error: {ex.Message}");
            }
        }

        public bool AddSupplier(Supplier supplier)
        {
            if (supplier == null) return false;
            if (string.IsNullOrWhiteSpace(supplier.SupplierCode))
            {
                supplier.SupplierCode = $"SUP-{new Random().Next(100, 999)}";
            }
            supplier.CreatedAt = DateTime.UtcNow;
            supplier.IsActive = true;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.CreateSupplierAsync(ActiveCompanyId, supplier)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddSupplier API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.AddSupplierAsync(ActiveCompanyId, supplier, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var saved = Task.Run(() => _localDb.AddSupplierAsync(ActiveCompanyId, supplier, enqueueSync: true)).GetAwaiter().GetResult();
                    if (saved != null && saved.SupplierId > 0)
                    {
                        supplier.SupplierId = saved.SupplierId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddSupplier localDb error: {ex.Message}");
                    return false;
                }
            }

            if (supplier.SupplierId == 0)
            {
                supplier.SupplierId = (Suppliers.Count > 0 ? Suppliers.Max(s => s.SupplierId) : 0) + 1;
            }

            Suppliers.Add(supplier);
            SaveSuppliersToLocalCache();
            SuppliersChanged?.Invoke();
            return true;
        }

        public bool UpdateSupplier(Supplier supplier)
        {
            if (supplier == null) return false;
            var existing = Suppliers.FirstOrDefault(s => s.SupplierId == supplier.SupplierId);
            if (existing == null) return false;

            var updatedSupplier = new Supplier
            {
                SupplierId = existing.SupplierId,
                SupplierCode = existing.SupplierCode,
                SupplierName = supplier.SupplierName,
                ContactPerson = supplier.ContactPerson,
                ContactNumber = supplier.ContactNumber,
                EmailAddress = supplier.EmailAddress,
                Address = supplier.Address,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt
            };

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdateSupplierAsync(ActiveCompanyId, supplier.SupplierId, updatedSupplier)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateSupplier API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateSupplierAsync(ActiveCompanyId, updatedSupplier, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateSupplierAsync(ActiveCompanyId, updatedSupplier, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateSupplier localDb error: {ex.Message}");
                    return false;
                }
            }

            existing.SupplierName = updatedSupplier.SupplierName;
            existing.ContactPerson = updatedSupplier.ContactPerson;
            existing.ContactNumber = updatedSupplier.ContactNumber;
            existing.EmailAddress = updatedSupplier.EmailAddress;
            existing.Address = updatedSupplier.Address;

            SaveSuppliersToLocalCache();
            SuppliersChanged?.Invoke();
            return true;
        }

        public bool ToggleSupplierArchive(int supplierId)
        {
            var existing = Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
            if (existing == null) return false;

            bool targetActive = !existing.IsActive;
            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    if (!targetActive)
                        apiSuccess = Task.Run(() => _apiClient.DeleteSupplierAsync(ActiveCompanyId, supplierId)).GetAwaiter().GetResult();
                    else
                    {
                        var copy = new Supplier
                        {
                            SupplierId = existing.SupplierId,
                            SupplierCode = existing.SupplierCode,
                            SupplierName = existing.SupplierName,
                            ContactPerson = existing.ContactPerson,
                            ContactNumber = existing.ContactNumber,
                            EmailAddress = existing.EmailAddress,
                            Address = existing.Address,
                            IsActive = true,
                            CreatedAt = existing.CreatedAt
                        };
                        apiSuccess = Task.Run(() => _apiClient.UpdateSupplierAsync(ActiveCompanyId, supplierId, copy)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleSupplierArchive API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    if (!targetActive)
                    {
                        Task.Run(() => _localDb.DeleteSupplierAsync(ActiveCompanyId, supplierId, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var copy = new Supplier
                        {
                            SupplierId = existing.SupplierId,
                            SupplierCode = existing.SupplierCode,
                            SupplierName = existing.SupplierName,
                            ContactPerson = existing.ContactPerson,
                            ContactNumber = existing.ContactNumber,
                            EmailAddress = existing.EmailAddress,
                            Address = existing.Address,
                            IsActive = true,
                            CreatedAt = existing.CreatedAt
                        };
                        Task.Run(() => _localDb.UpdateSupplierAsync(ActiveCompanyId, copy, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!targetActive)
                    {
                        Task.Run(() => _localDb.DeleteSupplierAsync(ActiveCompanyId, supplierId, enqueueSync: true)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var copy = new Supplier
                        {
                            SupplierId = existing.SupplierId,
                            SupplierCode = existing.SupplierCode,
                            SupplierName = existing.SupplierName,
                            ContactPerson = existing.ContactPerson,
                            ContactNumber = existing.ContactNumber,
                            EmailAddress = existing.EmailAddress,
                            Address = existing.Address,
                            IsActive = true,
                            CreatedAt = existing.CreatedAt
                        };
                        Task.Run(() => _localDb.UpdateSupplierAsync(ActiveCompanyId, copy, enqueueSync: true)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleSupplierArchive localDb error: {ex.Message}");
                    return false;
                }
            }

            existing.IsActive = targetActive;
            SaveSuppliersToLocalCache();
            SuppliersChanged?.Invoke();
            return true;
        }

        public bool DeleteSupplier(int supplierId) => ToggleSupplierArchive(supplierId);

        // =========================================================================
        // TENANT B: STAFF MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalStaffFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_staff.json");
        }

        public void SaveStaffToLocalCache()
        {
            try
            {
                string path = GetLocalStaffFilePath();
                string json = JsonSerializer.Serialize(StaffMembers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveStaffToLocalCache error: {ex.Message}");
            }
        }

        public void LoadStaffFromLocalCache()
        {
            try
            {
                string path = GetLocalStaffFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<StaffMember>>(json);
                    if (cached != null)
                    {
                        StaffMembers = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStaffFromLocalCache error: {ex.Message}");
            }
        }

        public bool AddStaffMember(StaffMember staff)
        {
            if (staff == null) return false;
            staff.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(staff.StaffCode))
            {
                staff.StaffCode = $"EMP-{new Random().Next(1000, 9999)}";
            }
            staff.HiredDate = DateTime.UtcNow;
            staff.IsActive = true;

            if (!string.IsNullOrWhiteSpace(staff.InitialPassword))
            {
                OfflineAuthService.Instance.RegisterOrUpdateStaffPassword(
                    ActiveCompanyId,
                    CurrentCompany.CompanyCode,
                    CurrentCompany.CompanyName,
                    CurrentCompany.PlanName,
                    staff.Username,
                    staff.FullName,
                    staff.Role,
                    staff.InitialPassword
                );
            }

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.CreateStaffAsync(ActiveCompanyId, staff)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddStaffMember API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.AddStaffMemberAsync(ActiveCompanyId, staff, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var saved = Task.Run(() => _localDb.AddStaffMemberAsync(ActiveCompanyId, staff, enqueueSync: true)).GetAwaiter().GetResult();
                    if (saved != null && saved.StaffId > 0)
                    {
                        staff.StaffId = saved.StaffId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddStaffMember localDb error: {ex.Message}");
                    return false;
                }
            }

            if (staff.StaffId == 0)
            {
                staff.StaffId = (StaffMembers.Count > 0 ? StaffMembers.Max(s => s.StaffId) : 0) + 1;
            }

            StaffMembers.Add(staff);
            SaveStaffToLocalCache();
            StaffMembersChanged?.Invoke();
            return true;
        }

        public bool UpdateStaffMember(StaffMember staff)
        {
            if (staff == null) return false;
            var existing = StaffMembers.FirstOrDefault(s => s.StaffId == staff.StaffId);
            if (existing == null) return false;

            var updatedStaff = new StaffMember
            {
                StaffId = existing.StaffId,
                CompanyId = existing.CompanyId,
                StaffCode = existing.StaffCode,
                FullName = staff.FullName,
                Username = existing.Username,
                Role = staff.Role,
                PositionTitle = staff.PositionTitle,
                Email = staff.Email,
                PhoneNumber = staff.PhoneNumber,
                HourlyRate = staff.HourlyRate,
                MonthlySalary = staff.MonthlySalary,
                IsActive = existing.IsActive,
                HiredDate = existing.HiredDate,
                InitialPassword = !string.IsNullOrWhiteSpace(staff.InitialPassword) ? staff.InitialPassword : existing.InitialPassword
            };

            if (!string.IsNullOrWhiteSpace(staff.InitialPassword))
            {
                OfflineAuthService.Instance.RegisterOrUpdateStaffPassword(
                    ActiveCompanyId,
                    CurrentCompany.CompanyCode,
                    CurrentCompany.CompanyName,
                    CurrentCompany.PlanName,
                    existing.Username,
                    existing.FullName,
                    existing.Role,
                    staff.InitialPassword
                );
            }

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdateStaffAsync(ActiveCompanyId, staff.StaffId, updatedStaff)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateStaffMember API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateStaffMemberAsync(ActiveCompanyId, updatedStaff, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateStaffMemberAsync(ActiveCompanyId, updatedStaff, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateStaffMember localDb error: {ex.Message}");
                    return false;
                }
            }

            existing.FullName = updatedStaff.FullName;
            existing.Role = updatedStaff.Role;
            existing.PositionTitle = updatedStaff.PositionTitle;
            existing.Email = updatedStaff.Email;
            existing.PhoneNumber = updatedStaff.PhoneNumber;
            existing.HourlyRate = updatedStaff.HourlyRate;
            existing.MonthlySalary = updatedStaff.MonthlySalary;
            if (!string.IsNullOrWhiteSpace(updatedStaff.InitialPassword))
            {
                existing.InitialPassword = updatedStaff.InitialPassword;
            }

            SaveStaffToLocalCache();
            StaffMembersChanged?.Invoke();
            return true;
        }

        public bool ToggleStaffArchive(int staffId)
        {
            var existing = StaffMembers.FirstOrDefault(s => s.StaffId == staffId);
            if (existing == null) return false;

            bool targetActive = !existing.IsActive;
            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    if (!targetActive)
                        apiSuccess = Task.Run(() => _apiClient.DeactivateStaffAsync(ActiveCompanyId, staffId)).GetAwaiter().GetResult();
                    else
                    {
                        var copy = new StaffMember
                        {
                            StaffId = existing.StaffId,
                            CompanyId = existing.CompanyId,
                            StaffCode = existing.StaffCode,
                            FullName = existing.FullName,
                            Username = existing.Username,
                            Role = existing.Role,
                            PositionTitle = existing.PositionTitle,
                            Email = existing.Email,
                            PhoneNumber = existing.PhoneNumber,
                            HourlyRate = existing.HourlyRate,
                            MonthlySalary = existing.MonthlySalary,
                            IsActive = true,
                            HiredDate = existing.HiredDate
                        };
                        apiSuccess = Task.Run(() => _apiClient.UpdateStaffAsync(ActiveCompanyId, staffId, copy)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleStaffArchive API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    if (!targetActive)
                    {
                        Task.Run(() => _localDb.DeleteStaffMemberAsync(ActiveCompanyId, staffId, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var copy = new StaffMember
                        {
                            StaffId = existing.StaffId,
                            CompanyId = existing.CompanyId,
                            StaffCode = existing.StaffCode,
                            FullName = existing.FullName,
                            Username = existing.Username,
                            Role = existing.Role,
                            PositionTitle = existing.PositionTitle,
                            Email = existing.Email,
                            PhoneNumber = existing.PhoneNumber,
                            HourlyRate = existing.HourlyRate,
                            MonthlySalary = existing.MonthlySalary,
                            IsActive = true,
                            HiredDate = existing.HiredDate
                        };
                        Task.Run(() => _localDb.UpdateStaffMemberAsync(ActiveCompanyId, copy, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!targetActive)
                    {
                        Task.Run(() => _localDb.DeleteStaffMemberAsync(ActiveCompanyId, staffId, enqueueSync: true)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var copy = new StaffMember
                        {
                            StaffId = existing.StaffId,
                            CompanyId = existing.CompanyId,
                            StaffCode = existing.StaffCode,
                            FullName = existing.FullName,
                            Username = existing.Username,
                            Role = existing.Role,
                            PositionTitle = existing.PositionTitle,
                            Email = existing.Email,
                            PhoneNumber = existing.PhoneNumber,
                            HourlyRate = existing.HourlyRate,
                            MonthlySalary = existing.MonthlySalary,
                            IsActive = true,
                            HiredDate = existing.HiredDate
                        };
                        Task.Run(() => _localDb.UpdateStaffMemberAsync(ActiveCompanyId, copy, enqueueSync: true)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleStaffArchive localDb error: {ex.Message}");
                    return false;
                }
            }

            existing.IsActive = targetActive;
            SaveStaffToLocalCache();
            StaffMembersChanged?.Invoke();
            return true;
        }

        public bool DeactivateStaffMember(int staffId) => ToggleStaffArchive(staffId);

        // =========================================================================
        // TENANT B: WORKFLOW & APPROVAL CACHING & CRUD
        // =========================================================================
        private string GetLocalApprovalsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_approvals.json");
        }

        public void SaveApprovalsToLocalCache()
        {
            try
            {
                string path = GetLocalApprovalsFilePath();
                string json = JsonSerializer.Serialize(ApprovalRequests, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveApprovalsToLocalCache error: {ex.Message}");
            }
        }

        public void LoadApprovalsFromLocalCache()
        {
            try
            {
                string path = GetLocalApprovalsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<ApprovalRequest>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        ApprovalRequests = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadApprovalsFromLocalCache error: {ex.Message}");
            }
        }

        public bool AddApprovalRequest(ApprovalRequest request)
        {
            if (request == null) return false;
            request.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(request.RequestNumber))
            {
                request.RequestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
            }
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "Pending";

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.CreateApprovalRequestAsync(ActiveCompanyId, request)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddApprovalRequest API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.CreateApprovalRequestAsync(ActiveCompanyId, request, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var saved = Task.Run(() => _localDb.CreateApprovalRequestAsync(ActiveCompanyId, request, enqueueSync: true)).GetAwaiter().GetResult();
                    if (saved != null && saved.RequestId > 0)
                    {
                        request.RequestId = saved.RequestId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddApprovalRequest localDb error: {ex.Message}");
                    return false;
                }
            }

            if (request.RequestId == 0)
            {
                request.RequestId = (ApprovalRequests.Count > 0 ? ApprovalRequests.Max(r => r.RequestId) : 0) + 1;
            }

            ApprovalRequests.Insert(0, request);
            SaveApprovalsToLocalCache();
            ApprovalRequestsChanged?.Invoke();
            return true;
        }

        public bool ResolveApprovalRequest(int requestId, string status, string reviewer, string? notes)
        {
            var request = ApprovalRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.ResolveApprovalRequestAsync(ActiveCompanyId, requestId, status, reviewer, notes)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ResolveApprovalRequest API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.ResolveApprovalRequestAsync(ActiveCompanyId, requestId, status, reviewer, notes, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.ResolveApprovalRequestAsync(ActiveCompanyId, requestId, status, reviewer, notes, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ResolveApprovalRequest localDb error: {ex.Message}");
                    return false;
                }
            }

            request.Status = status;
            request.ReviewedBy = reviewer;
            request.ReviewNotes = notes;
            request.ResolvedAt = DateTime.UtcNow;

            SaveApprovalsToLocalCache();
            ApprovalRequestsChanged?.Invoke();
            return true;
        }

        // =========================================================================
        // TENANT B: CUSTOMER MANAGEMENT CACHING & CRUD
        // =========================================================================
        private string GetLocalCustomersFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_customers.json");
        }

        public void SaveCustomersToLocalCache()
        {
            try
            {
                string path = GetLocalCustomersFilePath();
                string json = JsonSerializer.Serialize(Customers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCustomersToLocalCache error: {ex.Message}");
            }
        }

        public void LoadCustomersFromLocalCache()
        {
            try
            {
                string path = GetLocalCustomersFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<Customer>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        foreach (var c in cached)
                        {
                            if (string.IsNullOrWhiteSpace(c.Address) || !c.Address.Contains("Davao", StringComparison.OrdinalIgnoreCase))
                            {
                                c.Address = c.CustomerId switch
                                {
                                    1 => "J.P. Laurel Ave, Bajada, Davao City",
                                    2 => "McArthur Highway, Matina, Davao City",
                                    3 => "Lanang Business Park, Lanang, Davao City",
                                    4 => "Quimpo Blvd, Ecoland, Davao City",
                                    5 => "Roxas Ave, Poblacion District, Davao City",
                                    _ => string.IsNullOrWhiteSpace(c.Address) ? "Poblacion District, Davao City" : $"{c.Address}, Davao City"
                                };
                            }
                        }
                        Customers = cached;
                        SaveCustomersToLocalCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCustomersFromLocalCache error: {ex.Message}");
            }
        }

        public bool AddCustomer(Customer customer)
        {
            if (customer == null) return false;
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = $"CUST-{new Random().Next(1000, 9999)}";
            }
            customer.CreatedAt = DateTime.UtcNow;
            customer.IsActive = true;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.CreateCustomerAsync(ActiveCompanyId, customer)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddCustomer API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.AddCustomerAsync(ActiveCompanyId, customer, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var saved = Task.Run(() => _localDb.AddCustomerAsync(ActiveCompanyId, customer, enqueueSync: true)).GetAwaiter().GetResult();
                    if (saved != null && saved.CustomerId > 0)
                    {
                        customer.CustomerId = saved.CustomerId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddCustomer localDb error: {ex.Message}");
                    return false;
                }
            }

            if (customer.CustomerId == 0)
            {
                customer.CustomerId = (Customers.Count > 0 ? Customers.Max(c => c.CustomerId) : 0) + 1;
            }

            Customers.Add(customer);
            SaveCustomersToLocalCache();
            CustomersChanged?.Invoke();
            return true;
        }

        public bool UpdateCustomer(Customer customer)
        {
            if (customer == null) return false;
            var existing = Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (existing == null) return false;

            var updatedCustomer = new Customer
            {
                CustomerId = existing.CustomerId,
                CustomerCode = existing.CustomerCode,
                CustomerName = customer.CustomerName,
                ContactNumber = customer.ContactNumber,
                EmailAddress = customer.EmailAddress,
                Address = customer.Address,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt
            };

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdateCustomerAsync(ActiveCompanyId, customer.CustomerId, updatedCustomer)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCustomer API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateCustomerAsync(ActiveCompanyId, updatedCustomer, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateCustomerAsync(ActiveCompanyId, updatedCustomer, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCustomer localDb error: {ex.Message}");
                    return false;
                }
            }

            existing.CustomerName = updatedCustomer.CustomerName;
            existing.ContactNumber = updatedCustomer.ContactNumber;
            existing.EmailAddress = updatedCustomer.EmailAddress;
            existing.Address = updatedCustomer.Address;

            SaveCustomersToLocalCache();
            CustomersChanged?.Invoke();
            return true;
        }

        public bool ToggleCustomerArchive(int customerId)
        {
            var existing = Customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (existing == null) return false;

            bool targetActive = !existing.IsActive;
            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    if (!targetActive)
                        apiSuccess = Task.Run(() => _apiClient.DeleteCustomerAsync(ActiveCompanyId, customerId)).GetAwaiter().GetResult();
                    else
                    {
                        var copy = new Customer
                        {
                            CustomerId = existing.CustomerId,
                            CustomerCode = existing.CustomerCode,
                            CustomerName = existing.CustomerName,
                            ContactNumber = existing.ContactNumber,
                            EmailAddress = existing.EmailAddress,
                            Address = existing.Address,
                            IsActive = true,
                            CreatedAt = existing.CreatedAt
                        };
                        apiSuccess = Task.Run(() => _apiClient.UpdateCustomerAsync(ActiveCompanyId, customerId, copy)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleCustomerArchive API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    if (!targetActive)
                    {
                        Task.Run(() => _localDb.DeleteCustomerAsync(ActiveCompanyId, customerId, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var copy = new Customer
                        {
                            CustomerId = existing.CustomerId,
                            CustomerCode = existing.CustomerCode,
                            CustomerName = existing.CustomerName,
                            ContactNumber = existing.ContactNumber,
                            EmailAddress = existing.EmailAddress,
                            Address = existing.Address,
                            IsActive = true,
                            CreatedAt = existing.CreatedAt
                        };
                        Task.Run(() => _localDb.UpdateCustomerAsync(ActiveCompanyId, copy, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!targetActive)
                    {
                        Task.Run(() => _localDb.DeleteCustomerAsync(ActiveCompanyId, customerId, enqueueSync: true)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var copy = new Customer
                        {
                            CustomerId = existing.CustomerId,
                            CustomerCode = existing.CustomerCode,
                            CustomerName = existing.CustomerName,
                            ContactNumber = existing.ContactNumber,
                            EmailAddress = existing.EmailAddress,
                            Address = existing.Address,
                            IsActive = true,
                            CreatedAt = existing.CreatedAt
                        };
                        Task.Run(() => _localDb.UpdateCustomerAsync(ActiveCompanyId, copy, enqueueSync: true)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleCustomerArchive localDb error: {ex.Message}");
                    return false;
                }
            }

            existing.IsActive = targetActive;
            SaveCustomersToLocalCache();
            CustomersChanged?.Invoke();
            return true;
        }

        public bool DeleteCustomer(int customerId) => ToggleCustomerArchive(customerId);

        public decimal GetTotalRevenue() => Orders.Sum(o => o.TotalAmount);
        public int GetTotalOrders() => Orders.Count;
        public int GetTotalProductsCount() => Products.Count;
        public int GetLowStockCount() => Products.Count(p => p.IsLowStock);

        // =========================================================================
        // TENANT B: STORE PAYROLL CALCULATOR CACHING & CRUD
        // =========================================================================
        private string GetLocalPayrollFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_payroll.json");
        }

        public void SavePayrollToLocalCache()
        {
            try
            {
                string path = GetLocalPayrollFilePath();
                string json = JsonSerializer.Serialize(PayrollRecords, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SavePayrollToLocalCache error: {ex.Message}");
            }
        }

        public void LoadPayrollFromLocalCache()
        {
            try
            {
                string path = GetLocalPayrollFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<PayrollRecord>>(json);
                    if (cached != null)
                    {
                        PayrollRecords = cached.OrderByDescending(p => p.ProcessedAt).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPayrollFromLocalCache error: {ex.Message}");
            }
        }

        public bool AddPayrollRecord(PayrollRecord record)
        {
            if (record == null) return false;
            record.CompanyId = ActiveCompanyId;
            record.ProcessedAt = DateTime.UtcNow;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.CreatePayrollRecordAsync(ActiveCompanyId, record)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddPayrollRecord API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.AddPayrollRecordAsync(ActiveCompanyId, record, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    var saved = Task.Run(() => _localDb.AddPayrollRecordAsync(ActiveCompanyId, record, enqueueSync: true)).GetAwaiter().GetResult();
                    if (saved != null && saved.PayrollId > 0)
                    {
                        record.PayrollId = saved.PayrollId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddPayrollRecord localDb error: {ex.Message}");
                    return false;
                }
            }

            if (record.PayrollId == 0)
            {
                record.PayrollId = (PayrollRecords.Count > 0 ? PayrollRecords.Max(p => p.PayrollId) : 0) + 1;
            }

            PayrollRecords.Insert(0, record);
            SavePayrollToLocalCache();
            PayrollRecordsChanged?.Invoke();
            return true;
        }

        public bool DeletePayrollRecord(int payrollId)
        {
            var record = PayrollRecords.FirstOrDefault(p => p.PayrollId == payrollId);
            if (record != null)
            {
                PayrollRecords.Remove(record);
                SavePayrollToLocalCache();
                PayrollRecordsChanged?.Invoke();
                return true;
            }
            return false;
        }

        // =========================================================================
        // TENANT B: TERMS, POLICIES & AGREEMENTS CACHING & CRUD
        // =========================================================================
        private string GetLocalPoliciesFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_policies.json");
        }

        public void SavePoliciesToLocalCache()
        {
            try
            {
                string path = GetLocalPoliciesFilePath();
                string json = JsonSerializer.Serialize(StorePolicies, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SavePoliciesToLocalCache error: {ex.Message}");
            }
        }

        public void LoadPoliciesFromLocalCache()
        {
            try
            {
                string path = GetLocalPoliciesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<StorePolicy>>(json);
                    if (cached != null)
                    {
                        StorePolicies = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPoliciesFromLocalCache error: {ex.Message}");
            }
        }

        public StorePolicy? GetPolicy(string policyType)
        {
            return StorePolicies.FirstOrDefault(p => string.Equals(p.PolicyType, policyType, StringComparison.OrdinalIgnoreCase));
        }

        public bool UpdateStorePolicy(string policyType, string content, string updatedBy)
        {
            var policy = StorePolicies.FirstOrDefault(p => string.Equals(p.PolicyType, policyType, StringComparison.OrdinalIgnoreCase));
            if (policy == null) return false;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.UpdatePolicyAsync(ActiveCompanyId, policyType, content, updatedBy)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateStorePolicy API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (!apiSuccess)
                {
                    return false;
                }

                try
                {
                    Task.Run(() => _localDb.UpdateStorePolicyAsync(ActiveCompanyId, policyType, content, updatedBy, enqueueSync: false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            else
            {
                try
                {
                    Task.Run(() => _localDb.UpdateStorePolicyAsync(ActiveCompanyId, policyType, content, updatedBy, enqueueSync: true)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateStorePolicy localDb error: {ex.Message}");
                    return false;
                }
            }

            policy.ContentText = content;
            policy.LastUpdatedBy = updatedBy;
            policy.UpdatedAt = DateTime.UtcNow;

            SavePoliciesToLocalCache();
            StorePoliciesChanged?.Invoke();
            return true;
        }

        #region Business Intelligence Analytics
        public List<Order> GetOrdersForTimeRange(BiTimeRange range)
        {
            var now = DateTime.Now;
            return range switch
            {
                BiTimeRange.Today => Orders.Where(o => o.CreatedAt.Date == now.Date).ToList(),
                BiTimeRange.ThisWeek => Orders.Where(o => o.CreatedAt.Date >= now.Date.AddDays(-6)).ToList(),
                BiTimeRange.ThisMonth => Orders.Where(o => o.CreatedAt.Year == now.Year && o.CreatedAt.Month == now.Month).ToList(),
                _ => Orders.ToList()
            };
        }

        public BiSalesMetrics GetBiSalesMetrics(BiTimeRange range)
        {
            var orders = GetOrdersForTimeRange(range);
            decimal revenue = orders.Sum(o => o.TotalAmount);
            int count = orders.Count;
            int itemsSold = orders.SelectMany(o => o.Items).Sum(i => i.Quantity);

            // Estimate profit margin at ~28.5% typical for PC hardware retail
            decimal grossProfit = revenue * 0.285m;

            // Calculate growth compared to prior equivalent period
            decimal priorRevenue = 0m;
            var now = DateTime.Now;
            if (range == BiTimeRange.Today)
            {
                priorRevenue = Orders.Where(o => o.CreatedAt.Date == now.Date.AddDays(-1)).Sum(o => o.TotalAmount);
            }
            else if (range == BiTimeRange.ThisWeek)
            {
                priorRevenue = Orders.Where(o => o.CreatedAt.Date >= now.Date.AddDays(-13) && o.CreatedAt.Date < now.Date.AddDays(-6)).Sum(o => o.TotalAmount);
            }
            else if (range == BiTimeRange.ThisMonth)
            {
                var prevMonth = now.AddMonths(-1);
                priorRevenue = Orders.Where(o => o.CreatedAt.Year == prevMonth.Year && o.CreatedAt.Month == prevMonth.Month).Sum(o => o.TotalAmount);
            }

            decimal growthRate = 0m;
            if (priorRevenue > 0)
            {
                growthRate = ((revenue - priorRevenue) / priorRevenue) * 100m;
            }
            else if (revenue > 0)
            {
                growthRate = 100m;
            }

            return new BiSalesMetrics
            {
                TotalRevenue = revenue,
                OrderCount = count,
                TotalItemsSold = itemsSold,
                GrossProfit = grossProfit,
                GrowthRate = growthRate
            };
        }

        public List<BiCategoryShare> GetBiCategoryDistribution(BiTimeRange range)
        {
            var orders = GetOrdersForTimeRange(range);
            var categoryMap = Products.ToDictionary(p => p.ProductId, p => p.CategoryName);

            var list = new Dictionary<string, (decimal Revenue, int Units)>(StringComparer.OrdinalIgnoreCase);

            foreach (var o in orders)
            {
                foreach (var item in o.Items)
                {
                    string cat = "Peripherals";
                    if (categoryMap.TryGetValue(item.ProductId, out var mappedCat) && !string.IsNullOrWhiteSpace(mappedCat))
                    {
                        cat = mappedCat;
                    }
                    else
                    {
                        var prod = Products.FirstOrDefault(p => p.Name.Equals(item.ProductName, StringComparison.OrdinalIgnoreCase));
                        if (prod != null && !string.IsNullOrWhiteSpace(prod.CategoryName))
                        {
                            cat = prod.CategoryName;
                        }
                    }

                    cat = cat.Trim();
                    (decimal Revenue, int Units) cur = list.TryGetValue(cat, out var existing) ? existing : (0m, 0);
                    list[cat] = (cur.Revenue + item.TotalPrice, cur.Units + item.Quantity);
                }
            }

            decimal totalRevenue = list.Values.Sum(v => v.Revenue);
            if (totalRevenue == 0m) totalRevenue = 1m;

            return list
                .Select(kvp => new BiCategoryShare
                {
                    CategoryName = kvp.Key,
                    Revenue = kvp.Value.Revenue,
                    UnitsSold = kvp.Value.Units,
                    Percentage = Math.Round((kvp.Value.Revenue / totalRevenue) * 100m, 1)
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();
        }

        public List<BiProductPerformance> GetBiBestSellers(BiTimeRange range, int topN = 5)
        {
            var orders = GetOrdersForTimeRange(range);
            var grouped = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId > 0 ? i.ProductId.ToString() : i.ProductName)
                .Select(g =>
                {
                    var first = g.First();
                    var prod = Products.FirstOrDefault(p => p.ProductId == first.ProductId || p.Name.Equals(first.ProductName, StringComparison.OrdinalIgnoreCase));
                    decimal rev = g.Sum(x => x.TotalPrice);
                    int qty = g.Sum(x => x.Quantity);
                    return new BiProductPerformance
                    {
                        ProductId = prod?.ProductId ?? first.ProductId,
                        ProductName = prod?.Name ?? first.ProductName,
                        CategoryName = prod?.CategoryName ?? "Hardware",
                        Revenue = rev,
                        UnitsSold = qty,
                        CurrentStock = prod?.StockQuantity ?? 0,
                        UnitPrice = prod?.Price ?? first.UnitPrice
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .Take(topN)
                .ToList();

            return grouped;
        }

        public BiStockTrends GetBiStockTrends()
        {
            int totalItems = Products.Count;
            int totalStock = Products.Sum(p => p.StockQuantity);
            decimal valuation = Products.Sum(p => p.StockQuantity * p.Price);

            int healthy = Products.Count(p => p.StockQuantity > 5);
            int low = Products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
            int outOfStock = Products.Count(p => p.StockQuantity == 0);

            var bestSellersAll = GetBiBestSellers(BiTimeRange.AllTime, 5);
            var slowMoving = Products
                .Where(p => p.StockQuantity > 5 && !bestSellersAll.Any(b => b.ProductId == p.ProductId))
                .Take(5)
                .ToList();

            return new BiStockTrends
            {
                TotalCatalogItems = totalItems,
                TotalStockUnits = totalStock,
                TotalAssetValuation = valuation,
                HealthyStockCount = healthy,
                LowStockCount = low,
                OutOfStockCount = outOfStock,
                FastMovingProducts = bestSellersAll,
                SlowMovingProducts = slowMoving
            };
        }

        public BiEarningsSummary GetBiEarningsSummary()
        {
            var now = DateTime.Now;
            var monthOrders = Orders.Where(o => o.CreatedAt.Year == now.Year && o.CreatedAt.Month == now.Month).ToList();
            decimal grossRevenue = monthOrders.Sum(o => o.TotalAmount);
            decimal estimatedCogs = grossRevenue * 0.715m;
            decimal grossProfit = grossRevenue - estimatedCogs;
            decimal marginPercent = grossRevenue > 0 ? (grossProfit / grossRevenue) * 100m : 28.5m;

            int dayOfMonth = Math.Max(1, now.Day);
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            decimal dailyRunRate = grossRevenue / dayOfMonth;
            decimal projected = dailyRunRate * daysInMonth;

            return new BiEarningsSummary
            {
                GrossRevenue = grossRevenue,
                EstimatedCogs = estimatedCogs,
                GrossProfit = grossProfit,
                MarginPercent = marginPercent,
                ProjectedMonthEnd = projected,
                TotalTransactions = monthOrders.Count
            };
        }
        #endregion
    }

    public enum BiTimeRange
    {
        Today,
        ThisWeek,
        ThisMonth,
        AllTime
    }

    public class BiSalesMetrics
    {
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue => OrderCount > 0 ? TotalRevenue / OrderCount : 0m;
        public int TotalItemsSold { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin => TotalRevenue > 0 ? (GrossProfit / TotalRevenue) * 100m : 0m;
    }

    public class BiCategoryShare
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
        public decimal Percentage { get; set; }
    }

    public class BiProductPerformance
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
        public int CurrentStock { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class BiStockTrends
    {
        public int TotalCatalogItems { get; set; }
        public int TotalStockUnits { get; set; }
        public decimal TotalAssetValuation { get; set; }
        public int HealthyStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal InStockRate => TotalCatalogItems > 0 ? (decimal)(HealthyStockCount + LowStockCount) / TotalCatalogItems * 100m : 100m;
        public List<BiProductPerformance> FastMovingProducts { get; set; } = new();
        public List<Product> SlowMovingProducts { get; set; } = new();
    }

    public class BiEarningsSummary
    {
        public decimal GrossRevenue { get; set; }
        public decimal EstimatedCogs { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal ProjectedMonthEnd { get; set; }
        public int TotalTransactions { get; set; }
    }

    public partial class DataService
    {
        // =========================================================================
        // TENANT B: FINANCE & ACCOUNTING / EXPENSES CACHING & CRUD
        // =========================================================================
        private string GetLocalExpensesFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{ActiveCompanyId}_expenses.json");
        }

        public void SaveExpensesToLocalCache()
        {
            try
            {
                string path = GetLocalExpensesFilePath();
                string json = JsonSerializer.Serialize(ExpenseRecords, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveExpensesToLocalCache error: {ex.Message}");
            }
        }

        public void LoadExpensesFromLocalCache()
        {
            try
            {
                string path = GetLocalExpensesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cached = JsonSerializer.Deserialize<List<ExpenseRecord>>(json);
                    if (cached != null)
                    {
                        ExpenseRecords = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadExpensesFromLocalCache error: {ex.Message}");
            }
        }

        private string? ResolveLocalExpensesFilePath(int companyId)
        {
            string fileName = $"tenant_{companyId}_expenses.json";
            string[] searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "LocalData", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "ERP.winforms", "bin", "Debug", "net10.0-windows", "LocalData", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "ERP.winforms", "LocalData", fileName)
            };

            foreach (var p in searchPaths)
            {
                try
                {
                    if (File.Exists(p)) return Path.GetFullPath(p);
                }
                catch { }
            }
            return null;
        }

        public void MigrateJsonExpensesIfAvailable(int companyId)
        {
            try
            {
                string? path = ResolveLocalExpensesFilePath(companyId);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

                string json = File.ReadAllText(path);
                var jsonRecords = JsonSerializer.Deserialize<List<ExpenseRecord>>(json);
                if (jsonRecords == null || jsonRecords.Count == 0) return;

                var existingInDb = Task.Run(() => _localDb.GetExpensesAsync(companyId)).GetAwaiter().GetResult();
                if (existingInDb != null && existingInDb.Count > 0)
                {
                    ExpenseRecords = existingInDb;
                    return;
                }

                foreach (var rec in jsonRecords)
                {
                    rec.CompanyId = companyId;
                    rec.ExpenseId = 0;
                    Task.Run(() => _localDb.SaveExpenseAsync(companyId, rec, enqueueSync: false)).GetAwaiter().GetResult();
                }

                ExpenseRecords = Task.Run(() => _localDb.GetExpensesAsync(companyId)).GetAwaiter().GetResult() ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MigrateJsonExpensesIfAvailable error: {ex.Message}");
            }
        }

        public bool AddExpense(ExpenseRecord expense)
        {
            if (expense == null) return false;
            expense.CompanyId = ActiveCompanyId;
            if (string.IsNullOrWhiteSpace(expense.ExpenseNumber))
            {
                expense.ExpenseNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
            }
            if (expense.ExpenseDate == default)
            {
                expense.ExpenseDate = DateTime.UtcNow;
            }

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                ExpenseRecord? apiCreated = null;
                try
                {
                    apiCreated = Task.Run(() => _apiClient.CreateExpenseAsync(ActiveCompanyId, expense)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddExpense API error: {ex.Message}");
                    apiCreated = null;
                }

                if (apiCreated != null)
                {
                    try
                    {
                        Task.Run(() => _localDb.SaveExpenseAsync(ActiveCompanyId, apiCreated, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                    catch { }

                    ExpenseRecords.RemoveAll(e => e.ExpenseId == apiCreated.ExpenseId);
                    ExpenseRecords.Insert(0, apiCreated);
                    ExpensesChanged?.Invoke();
                    return true;
                }
            }

            // Offline or API failure: persist locally and enqueue SyncOutbox
            try
            {
                var saved = Task.Run(() => _localDb.SaveExpenseAsync(ActiveCompanyId, expense, enqueueSync: true)).GetAwaiter().GetResult();
                if (saved != null)
                {
                    expense.ExpenseId = saved.ExpenseId;
                }
                ExpenseRecords.RemoveAll(e => e.ExpenseId == expense.ExpenseId);
                ExpenseRecords.Insert(0, expense);
                ExpensesChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddExpense localDb error: {ex.Message}");
                return false;
            }
        }

        public bool ToggleExpenseArchive(int expenseId)
        {
            var exp = ExpenseRecords.FirstOrDefault(e => e.ExpenseId == expenseId);
            if (exp == null) return false;

            bool targetActive = !exp.IsActive;

            bool isOnline = IsApiReachable();
            if (isOnline)
            {
                bool apiSuccess = false;
                try
                {
                    apiSuccess = Task.Run(() => _apiClient.ArchiveExpenseAsync(ActiveCompanyId, expenseId)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToggleExpenseArchive API error: {ex.Message}");
                    apiSuccess = false;
                }

                if (apiSuccess)
                {
                    try
                    {
                        Task.Run(() => _localDb.ToggleExpenseArchiveAsync(ActiveCompanyId, expenseId, enqueueSync: false)).GetAwaiter().GetResult();
                    }
                    catch { }

                    exp.IsActive = targetActive;
                    ExpensesChanged?.Invoke();
                    return true;
                }
            }

            // Offline or API failure: persist locally and enqueue SyncOutbox
            try
            {
                Task.Run(() => _localDb.ToggleExpenseArchiveAsync(ActiveCompanyId, expenseId, enqueueSync: true)).GetAwaiter().GetResult();
                exp.IsActive = targetActive;
                ExpensesChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleExpenseArchive localDb error: {ex.Message}");
                return false;
            }
        }

        public bool DeleteExpense(int expenseId) => ToggleExpenseArchive(expenseId);

        public decimal GetTotalRetailSalesRevenue()
        {
            return Orders.Where(o => !string.Equals(o.Status, "Voided", StringComparison.OrdinalIgnoreCase)).Sum(o => o.TotalAmount);
        }

        public decimal GetTotalRepairServicesRevenue()
        {
            return RepairTickets.Where(t => string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Status, "Released", StringComparison.OrdinalIgnoreCase)).Sum(t => t.TotalAmount);
        }

        public decimal GetTotalPayrollExpense()
        {
            return PayrollRecords.Sum(p => p.NetPay);
        }

        public decimal GetTotalOperatingExpenses()
        {
            return ExpenseRecords.Where(e => e.IsActive).Sum(e => e.Amount);
        }

        public decimal GetTotalVatCollected()
        {
            return Orders.Where(o => !string.Equals(o.Status, "Voided", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Tax);
        }
    }
}
