using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.domain.entities;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Background Outbox & Auto-Sync Manager for offline operations.
    /// Operates against the local SQL Server SyncOutbox table and synchronizes pending
    /// operations to MonsterASP via ERP.api when network connectivity is available.
    /// </summary>
    public class SyncManager
    {
        private static SyncManager? _instance;
        public static SyncManager Instance => _instance ??= new SyncManager();

        private readonly LocalDatabaseService _localDb;
        private readonly ApiClient _apiClient;
        private readonly System.Threading.Timer _timer;
        private readonly JsonSerializerOptions _jsonOpts;
        private bool _isSyncing;

        public event Action<bool, int, string>? SyncStatusChanged;
        public int PendingCount { get; private set; }

        private SyncManager()
        {
            _localDb = LocalDatabaseService.Instance;
            _apiClient = ApiClient.Instance;
            _jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Periodic background sync checker every 25 seconds
            _timer = new System.Threading.Timer(async _ => await CheckAndSyncAsync().ConfigureAwait(false), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
        }

        /// <summary>
        /// Attempts to push all pending local SQL outbox items to ERP.api / Cloud database in the background,
        /// then continuously refreshes the local tenant database with the latest cloud state.
        /// Strict tenant isolation: Tenant A records only sync to Tenant A, and Tenant B only to Tenant B.
        /// </summary>
        public async Task CheckAndSyncAsync(int? specificCompanyId = null)
        {
            if (_isSyncing) return;

            // Check actual API connectivity rather than just network interface
            bool isConnected = await _apiClient.CheckApiConnectivityAsync().ConfigureAwait(false);
            if (!isConnected)
            {
                SyncStatusChanged?.Invoke(false, PendingCount, "Offline");
                return;
            }

            _isSyncing = true;
            SyncStatusChanged?.Invoke(true, PendingCount, "Syncing...");
            try
            {
                List<int> companiesToSync = specificCompanyId.HasValue
                    ? new List<int> { specificCompanyId.Value }
                    : new List<int> { 1, 2 };

                foreach (int companyId in companiesToSync)
                {
                    // 1. Process pending outbox first (Part 6)
                    List<SyncOutboxItem> pendingItems;
                    try
                    {
                        pendingItems = await _localDb.GetPendingOutboxItemsAsync(companyId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetPendingOutboxItems error for company {companyId}: {ex.Message}");
                        continue;
                    }

                    int syncedCount = 0;
                    if (pendingItems != null && pendingItems.Count > 0)
                    {
                        foreach (var item in pendingItems)
                        {
                            bool success = false;
                            string? failureReason = null;

                            try
                            {
                                success = await DispatchOutboxItemAsync(item).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                failureReason = ex.Message;
                                success = false;
                            }

                            if (success)
                            {
                                await _localDb.MarkOutboxSyncedAsync(item.CompanyId, item.SyncId).ConfigureAwait(false);
                                syncedCount++;
                            }
                            else
                            {
                                await _localDb.MarkOutboxFailedAsync(item.CompanyId, item.SyncId, failureReason ?? "API operation unsuccessful").ConfigureAwait(false);
                                // Network or API might be failing; abort current batch to prevent tight retries
                                break;
                            }
                        }
                    }

                    // 2. Perform Cloud -> Local refresh (Part 1, 2, 6)
                    try
                    {
                        await _localDb.RefreshFromCloudAsync(companyId, _apiClient).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"RefreshFromCloudAsync error for company {companyId}: {ex.Message}");
                    }

                    // 3. Update pending count & status
                    try
                    {
                        var remaining = await _localDb.GetPendingOutboxItemsAsync(companyId).ConfigureAwait(false);
                        PendingCount = remaining.Count;
                    }
                    catch
                    {
                        PendingCount = 0;
                    }
                }

                // If currently viewed tenant was refreshed, reload DataService in background to update views
                try
                {
                    if (DataService.Instance.ActiveCompanyId > 0)
                    {
                        _ = Task.Run(() => DataService.Instance.LoadFromDatabase());
                    }
                }
                catch { }

                SyncStatusChanged?.Invoke(true, PendingCount, "Online");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckAndSyncAsync general error: {ex.Message}");
                SyncStatusChanged?.Invoke(false, PendingCount, "Offline");
            }
            finally
            {
                _isSyncing = false;
            }
        }

        public void SetOffline()
        {
            SyncStatusChanged?.Invoke(false, PendingCount, "Offline");
        }

        private async Task<bool> DispatchOutboxItemAsync(SyncOutboxItem item)
        {
            switch (item.EntityType)
            {
                case "Order":
                    if (item.Operation == "Create")
                    {
                        var order = JsonSerializer.Deserialize<Order>(item.PayloadJson, _jsonOpts);
                        if (order == null) return false;
                        return await _apiClient.ProcessOrderAsync(item.CompanyId, order).ConfigureAwait(false);
                    }
                    break;

                case "Product":
                    if (item.Operation == "Create")
                    {
                        var product = JsonSerializer.Deserialize<Product>(item.PayloadJson, _jsonOpts);
                        if (product == null) return false;
                        var created = await _apiClient.AddProductAsync(item.CompanyId, product).ConfigureAwait(false);
                        return created != null;
                    }
                    else if (item.Operation == "Update")
                    {
                        var product = JsonSerializer.Deserialize<Product>(item.PayloadJson, _jsonOpts);
                        if (product == null) return false;
                        return await _apiClient.UpdateProductAsync(item.CompanyId, product).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Archive" && int.TryParse(item.EntityId, out int archiveId))
                    {
                        return await _apiClient.ArchiveProductAsync(item.CompanyId, archiveId).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Restore" && int.TryParse(item.EntityId, out int restoreId))
                    {
                        return await _apiClient.RestoreProductAsync(item.CompanyId, restoreId).ConfigureAwait(false);
                    }
                    break;

                case "Category":
                    if (item.Operation == "Create")
                    {
                        var cat = JsonSerializer.Deserialize<Category>(item.PayloadJson, _jsonOpts);
                        if (cat == null) return false;
                        var created = await _apiClient.AddCategoryAsync(item.CompanyId, cat).ConfigureAwait(false);
                        return created != null;
                    }
                    else if (item.Operation == "Update" && int.TryParse(item.EntityId, out int updateCatId))
                    {
                        var cat = JsonSerializer.Deserialize<Category>(item.PayloadJson, _jsonOpts);
                        if (cat == null) return false;
                        var updated = await _apiClient.UpdateCategoryAsync(item.CompanyId, updateCatId, cat).ConfigureAwait(false);
                        return updated != null;
                    }
                    else if (item.Operation == "Delete" && int.TryParse(item.EntityId, out int deleteCatId))
                    {
                        string? reassign = null;
                        try
                        {
                            using var doc = JsonDocument.Parse(item.PayloadJson);
                            if (doc.RootElement.TryGetProperty("ReassignFallback", out var rf))
                            {
                                reassign = rf.GetString();
                            }
                        }
                        catch { }
                        return await _apiClient.DeleteCategoryAsync(item.CompanyId, deleteCatId, reassign).ConfigureAwait(false);
                    }
                    break;

                case "Customer":
                    if (item.Operation == "Create")
                    {
                        var cust = JsonSerializer.Deserialize<Customer>(item.PayloadJson, _jsonOpts);
                        if (cust == null) return false;
                        return await _apiClient.CreateCustomerAsync(item.CompanyId, cust).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Update" && int.TryParse(item.EntityId, out int custId))
                    {
                        var cust = JsonSerializer.Deserialize<Customer>(item.PayloadJson, _jsonOpts);
                        if (cust == null) return false;
                        return await _apiClient.UpdateCustomerAsync(item.CompanyId, custId, cust).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Delete" && int.TryParse(item.EntityId, out int delCustId))
                    {
                        return await _apiClient.DeleteCustomerAsync(item.CompanyId, delCustId).ConfigureAwait(false);
                    }
                    break;

                case "Supplier":
                    if (item.Operation == "Create")
                    {
                        var sup = JsonSerializer.Deserialize<Supplier>(item.PayloadJson, _jsonOpts);
                        if (sup == null) return false;
                        return await _apiClient.CreateSupplierAsync(item.CompanyId, sup).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Update" && int.TryParse(item.EntityId, out int supId))
                    {
                        var sup = JsonSerializer.Deserialize<Supplier>(item.PayloadJson, _jsonOpts);
                        if (sup == null) return false;
                        return await _apiClient.UpdateSupplierAsync(item.CompanyId, supId, sup).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Delete" && int.TryParse(item.EntityId, out int delSupId))
                    {
                        return await _apiClient.DeleteSupplierAsync(item.CompanyId, delSupId).ConfigureAwait(false);
                    }
                    break;

                case "RepairTicket":
                    if (item.Operation == "Create")
                    {
                        var ticket = JsonSerializer.Deserialize<RepairTicket>(item.PayloadJson, _jsonOpts);
                        if (ticket == null) return false;
                        return await _apiClient.CreateRepairTicketAsync(item.CompanyId, ticket).ConfigureAwait(false);
                    }
                    else if (item.Operation == "UpdateStatus" && int.TryParse(item.EntityId, out int tId))
                    {
                        using var doc = JsonDocument.Parse(item.PayloadJson);
                        string status = doc.RootElement.GetProperty("Status").GetString()!;
                        string? notes = doc.RootElement.TryGetProperty("DiagnosticNotes", out var n) ? n.GetString() : null;
                        string? tech = doc.RootElement.TryGetProperty("AssignedTechnician", out var t) ? t.GetString() : null;
                        return await _apiClient.UpdateRepairStatusAsync(item.CompanyId, tId, status, notes, tech).ConfigureAwait(false);
                    }
                    else if (item.Operation == "UpdateBilling" && int.TryParse(item.EntityId, out int billId))
                    {
                        using var doc = JsonDocument.Parse(item.PayloadJson);
                        decimal labor = doc.RootElement.GetProperty("LaborFee").GetDecimal();
                        decimal parts = doc.RootElement.GetProperty("PartsCost").GetDecimal();
                        decimal deposit = doc.RootElement.GetProperty("DepositAmount").GetDecimal();
                        return await _apiClient.UpdateRepairBillingAsync(item.CompanyId, billId, labor, parts, deposit).ConfigureAwait(false);
                    }
                    break;

                case "StaffMember":
                    if (item.Operation == "Create")
                    {
                        var staff = JsonSerializer.Deserialize<StaffMember>(item.PayloadJson, _jsonOpts);
                        if (staff == null) return false;
                        return await _apiClient.CreateStaffAsync(item.CompanyId, staff).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Update" && int.TryParse(item.EntityId, out int staffId))
                    {
                        var staff = JsonSerializer.Deserialize<StaffMember>(item.PayloadJson, _jsonOpts);
                        if (staff == null) return false;
                        return await _apiClient.UpdateStaffAsync(item.CompanyId, staffId, staff).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Deactivate" && int.TryParse(item.EntityId, out int deactStaffId))
                    {
                        return await _apiClient.DeactivateStaffAsync(item.CompanyId, deactStaffId).ConfigureAwait(false);
                    }
                    break;

                case "ApprovalRequest":
                    if (item.Operation == "Create")
                    {
                        var req = JsonSerializer.Deserialize<ApprovalRequest>(item.PayloadJson, _jsonOpts);
                        if (req == null) return false;
                        return await _apiClient.CreateApprovalRequestAsync(item.CompanyId, req).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Resolve" && int.TryParse(item.EntityId, out int reqId))
                    {
                        using var doc = JsonDocument.Parse(item.PayloadJson);
                        string status = doc.RootElement.GetProperty("Status").GetString()!;
                        string reviewer = doc.RootElement.GetProperty("ReviewedBy").GetString()!;
                        string? notes = doc.RootElement.TryGetProperty("ReviewNotes", out var n) ? n.GetString() : null;
                        return await _apiClient.ResolveApprovalRequestAsync(item.CompanyId, reqId, status, reviewer, notes).ConfigureAwait(false);
                    }
                    break;

                case "PayrollRecord":
                    if (item.Operation == "Create")
                    {
                        var payroll = JsonSerializer.Deserialize<PayrollRecord>(item.PayloadJson, _jsonOpts);
                        if (payroll == null) return false;
                        return await _apiClient.CreatePayrollRecordAsync(item.CompanyId, payroll).ConfigureAwait(false);
                    }
                    break;

                case "StorePolicy":
                    if (item.Operation == "Update")
                    {
                        using var doc = JsonDocument.Parse(item.PayloadJson);
                        string policyType = doc.RootElement.GetProperty("PolicyType").GetString()!;
                        string content = doc.RootElement.GetProperty("ContentText").GetString()!;
                        string updatedBy = doc.RootElement.GetProperty("LastUpdatedBy").GetString()!;
                        return await _apiClient.UpdatePolicyAsync(item.CompanyId, policyType, content, updatedBy).ConfigureAwait(false);
                    }
                    break;

                case "Expense":
                    if (item.Operation == "Create")
                    {
                        var exp = JsonSerializer.Deserialize<ExpenseRecord>(item.PayloadJson, _jsonOpts);
                        if (exp == null) return false;
                        var created = await _apiClient.CreateExpenseAsync(item.CompanyId, exp).ConfigureAwait(false);
                        return created != null;
                    }
                    else if (item.Operation == "Update" && int.TryParse(item.EntityId, out int expId))
                    {
                        var exp = JsonSerializer.Deserialize<ExpenseRecord>(item.PayloadJson, _jsonOpts);
                        if (exp == null) return false;
                        return await _apiClient.UpdateExpenseAsync(item.CompanyId, expId, exp).ConfigureAwait(false);
                    }
                    else if (item.Operation == "Archive" && int.TryParse(item.EntityId, out int archExpId))
                    {
                        return await _apiClient.ArchiveExpenseAsync(item.CompanyId, archExpId).ConfigureAwait(false);
                    }
                    break;
            }

            return false;
        }

        // Backward compatibility wrappers that delegate directly to local DB
        public void EnqueueProduct(Product product, bool isUpdate, int companyId = 1)
        {
            _ = Task.Run(async () =>
            {
                await _localDb.SaveProductAsync(companyId, product, enqueueSync: true).ConfigureAwait(false);
                await CheckAndSyncAsync(companyId).ConfigureAwait(false);
            });
        }

        public void EnqueueArchiveProduct(int productId, int companyId = 1)
        {
            _ = Task.Run(async () =>
            {
                await _localDb.ArchiveProductAsync(companyId, productId, enqueueSync: true).ConfigureAwait(false);
                await CheckAndSyncAsync(companyId).ConfigureAwait(false);
            });
        }

        public void EnqueueRestoreProduct(int productId, int companyId = 1)
        {
            _ = Task.Run(async () =>
            {
                await _localDb.RestoreProductAsync(companyId, productId, enqueueSync: true).ConfigureAwait(false);
                await CheckAndSyncAsync(companyId).ConfigureAwait(false);
            });
        }

        public void EnqueueOrder(Order order, int companyId = 1)
        {
            _ = Task.Run(async () =>
            {
                await _localDb.ProcessOrderAsync(companyId, order, enqueueSync: true).ConfigureAwait(false);
                await CheckAndSyncAsync(companyId).ConfigureAwait(false);
            });
        }

        public void EnqueueCategory(Category category, bool isUpdate, int companyId = 1)
        {
            _ = Task.Run(async () =>
            {
                if (isUpdate)
                    await _localDb.UpdateCategoryAsync(companyId, category.Id, category.Name, category.Description, enqueueSync: true).ConfigureAwait(false);
                else
                    await _localDb.AddCategoryAsync(companyId, category, enqueueSync: true).ConfigureAwait(false);
                await CheckAndSyncAsync(companyId).ConfigureAwait(false);
            });
        }

        public void EnqueueDeleteCategory(int categoryId, string fallback, int companyId = 1)
        {
            _ = Task.Run(async () =>
            {
                await _localDb.DeleteCategoryAsync(companyId, categoryId, fallback, enqueueSync: true).ConfigureAwait(false);
                await CheckAndSyncAsync(companyId).ConfigureAwait(false);
            });
        }
    }
}
