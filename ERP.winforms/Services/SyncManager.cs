using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.domain.entities;

namespace ERP.winforms.Services
{
    public enum SyncActionType
    {
        CreateProduct,
        UpdateProduct,
        ArchiveProduct,
        RestoreProduct,
        CreateOrder,
        CreateCategory,
        UpdateCategory,
        DeleteCategory
    }

    public class SyncQueueItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public SyncActionType Action { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public int CompanyId { get; set; } = 1;
        public Product? ProductPayload { get; set; }
        public int TargetProductId { get; set; }
        public Order? OrderPayload { get; set; }
        public Category? CategoryPayload { get; set; }
        public int TargetCategoryId { get; set; }
        public string? ReassignFallback { get; set; }
    }

    /// <summary>
    /// Background Outbox & Auto-Sync Manager for offline operations.
    /// Stores unsynced changes to local disk and automatically pushes them to MonsterASP via ERP.api when internet returns.
    /// </summary>
    public class SyncManager
    {
        private static SyncManager? _instance;
        public static SyncManager Instance => _instance ??= new SyncManager();

        private readonly string _queueFilePath;
        private readonly List<SyncQueueItem> _queue = new();
        private readonly object _lock = new();
        private readonly System.Threading.Timer _timer;
        private bool _isSyncing;

        public event Action<bool, int, string>? SyncStatusChanged;

        public int PendingCount
        {
            get
            {
                lock (_lock) return _queue.Count;
            }
        }

        private SyncManager()
        {
            string appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _queueFilePath = Path.Combine(appData, "pending_sync.json");

            LoadQueueFromDisk();

            // Periodic background sync checker every 25 seconds
            _timer = new System.Threading.Timer(async _ => await CheckAndSyncAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(25));
        }

        private void LoadQueueFromDisk()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_queueFilePath))
                    {
                        string json = File.ReadAllText(_queueFilePath);
                        var items = JsonSerializer.Deserialize<List<SyncQueueItem>>(json);
                        if (items != null)
                        {
                            _queue.Clear();
                            _queue.AddRange(items);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadQueueFromDisk error: {ex.Message}");
                }
            }
        }

        private void SaveQueueToDisk()
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(_queue, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_queueFilePath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveQueueToDisk error: {ex.Message}");
                }
            }
        }

        public void EnqueueProduct(Product product, bool isUpdate, int companyId = 1)
        {
            lock (_lock)
            {
                // Remove existing queued update for same product to prevent redundant sync
                _queue.RemoveAll(q => q.ProductPayload != null &&
                    q.ProductPayload.ProductCode.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase));

                _queue.Add(new SyncQueueItem
                {
                    Action = isUpdate ? SyncActionType.UpdateProduct : SyncActionType.CreateProduct,
                    CompanyId = companyId,
                    ProductPayload = product
                });

                SaveQueueToDisk();
            }

            SyncStatusChanged?.Invoke(false, PendingCount, $"Offline change queued ({product.ProductName}). Will sync when online.");
        }

        public void EnqueueArchiveProduct(int productId, int companyId = 1)
        {
            lock (_lock)
            {
                _queue.RemoveAll(q => q.TargetProductId == productId &&
                    (q.Action == SyncActionType.ArchiveProduct || q.Action == SyncActionType.RestoreProduct));

                _queue.Add(new SyncQueueItem
                {
                    Action = SyncActionType.ArchiveProduct,
                    CompanyId = companyId,
                    TargetProductId = productId
                });

                SaveQueueToDisk();
            }

            SyncStatusChanged?.Invoke(false, PendingCount, "Product archive queued. Will sync when online.");
        }

        public void EnqueueRestoreProduct(int productId, int companyId = 1)
        {
            lock (_lock)
            {
                _queue.RemoveAll(q => q.TargetProductId == productId &&
                    (q.Action == SyncActionType.ArchiveProduct || q.Action == SyncActionType.RestoreProduct));

                _queue.Add(new SyncQueueItem
                {
                    Action = SyncActionType.RestoreProduct,
                    CompanyId = companyId,
                    TargetProductId = productId
                });

                SaveQueueToDisk();
            }

            SyncStatusChanged?.Invoke(false, PendingCount, "Product restore queued. Will sync when online.");
        }

        public void EnqueueOrder(Order order, int companyId = 1)
        {
            lock (_lock)
            {
                if (_queue.Any(q => q.OrderPayload != null && q.OrderPayload.Id == order.Id))
                {
                    return;
                }

                _queue.Add(new SyncQueueItem
                {
                    Action = SyncActionType.CreateOrder,
                    CompanyId = companyId,
                    OrderPayload = order
                });

                SaveQueueToDisk();
            }

            SyncStatusChanged?.Invoke(false, PendingCount, $"Order {order.Id} queued for cloud sync.");
        }

        public void EnqueueCategory(Category category, bool isUpdate, int companyId = 1)
        {
            lock (_lock)
            {
                _queue.RemoveAll(q => q.CategoryPayload != null && q.CategoryPayload.Id == category.Id);

                _queue.Add(new SyncQueueItem
                {
                    Action = isUpdate ? SyncActionType.UpdateCategory : SyncActionType.CreateCategory,
                    CompanyId = companyId,
                    CategoryPayload = category,
                    TargetCategoryId = category.Id
                });

                SaveQueueToDisk();
            }

            SyncStatusChanged?.Invoke(false, PendingCount, $"Category change queued ({category.Name}). Will sync when online.");
        }

        public void EnqueueDeleteCategory(int categoryId, string fallback, int companyId = 1)
        {
            lock (_lock)
            {
                _queue.RemoveAll(q => q.CategoryPayload != null && q.CategoryPayload.Id == categoryId);

                _queue.Add(new SyncQueueItem
                {
                    Action = SyncActionType.DeleteCategory,
                    CompanyId = companyId,
                    TargetCategoryId = categoryId,
                    ReassignFallback = fallback
                });

                SaveQueueToDisk();
            }

            SyncStatusChanged?.Invoke(false, PendingCount, $"Category deletion queued. Will sync when online.");
        }

        /// <summary>
        /// Attempts to push all pending items to ERP.api / MonsterASP in the background.
        /// </summary>
        public async Task CheckAndSyncAsync()
        {
            if (_isSyncing) return;

            List<SyncQueueItem> itemsToSync;
            lock (_lock)
            {
                if (_queue.Count == 0) return;
                itemsToSync = _queue.ToList();
            }

            _isSyncing = true;
            int syncedCount = 0;

            try
            {
                var apiClient = ApiClient.Instance;

                foreach (var item in itemsToSync)
                {
                    bool success = false;

                    switch (item.Action)
                    {
                        case SyncActionType.CreateProduct when item.ProductPayload != null:
                            var created = await apiClient.AddProductAsync(item.CompanyId, item.ProductPayload);
                            success = (created != null);
                            break;

                        case SyncActionType.UpdateProduct when item.ProductPayload != null:
                            success = await apiClient.UpdateProductAsync(item.CompanyId, item.ProductPayload);
                            break;

                        case SyncActionType.ArchiveProduct:
                            success = await apiClient.ArchiveProductAsync(item.CompanyId, item.TargetProductId);
                            break;

                        case SyncActionType.RestoreProduct:
                            success = await apiClient.RestoreProductAsync(item.CompanyId, item.TargetProductId);
                            break;

                        case SyncActionType.CreateOrder when item.OrderPayload != null:
                            success = await apiClient.ProcessOrderAsync(item.CompanyId, item.OrderPayload);
                            break;

                        case SyncActionType.CreateCategory when item.CategoryPayload != null:
                            var cat = await apiClient.AddCategoryAsync(item.CompanyId, item.CategoryPayload);
                            success = (cat != null);
                            break;

                        case SyncActionType.UpdateCategory when item.CategoryPayload != null:
                            var updated = await apiClient.UpdateCategoryAsync(item.CompanyId, item.TargetCategoryId, item.CategoryPayload);
                            success = (updated != null);
                            break;

                        case SyncActionType.DeleteCategory:
                            success = await apiClient.DeleteCategoryAsync(item.CompanyId, item.TargetCategoryId, item.ReassignFallback);
                            break;
                    }

                    if (success)
                    {
                        lock (_lock)
                        {
                            _queue.RemoveAll(q => q.Id == item.Id);
                            SaveQueueToDisk();
                        }
                        syncedCount++;
                    }
                    else
                    {
                        // Stop trying this cycle if network dropped
                        break;
                    }
                }

                if (syncedCount > 0)
                {
                    int remaining = PendingCount;
                    string msg = remaining == 0
                        ? $"All {syncedCount} offline changes successfully synced to MonsterASP cloud!"
                        : $"{syncedCount} items synced. {remaining} items remaining.";

                    SyncStatusChanged?.Invoke(true, remaining, msg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckAndSyncAsync error: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
