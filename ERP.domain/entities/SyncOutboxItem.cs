using System;

namespace ERP.domain.entities
{
    /// <summary>
    /// Represents a pending or synchronized offline operation record in the local database SyncOutbox.
    /// Used by the synchronization engine to replay local changes to the cloud database when online.
    /// </summary>
    public class SyncOutboxItem
    {
        public string SyncId { get; set; } = Guid.NewGuid().ToString("N");
        public int CompanyId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string SyncStatus { get; set; } = "Pending"; // Pending, Processing, Synced, Failed
        public int RetryCount { get; set; } = 0;
        public DateTime? LastAttemptAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
