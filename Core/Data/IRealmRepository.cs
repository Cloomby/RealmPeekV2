using RealmPeek.Core.Models;
using Realms;

namespace RealmPeek.Core.Data
{
    /// <summary>
    /// Repository interface for Realm database access
    /// Abstracts all Realm-specific operations
    /// </summary>
    public interface IRealmRepository : IDisposable
    {
        // Query operations
        List<BeatmapSetModel> LoadAllSets();
        BeatmapSetModel? FindSetByID(Guid id);
        BeatmapModel? FindBeatmapByQuery(string query, params QueryArgument[] args);

        // Diagnostic operations
        DiagnosticResult RunDiagnostics();

        // Write operations (requires write mode)
        void ExecuteWrite(Action<IWriteContext> action);
    }

    /// <summary>
    /// Write context for database modifications
    /// Provides safe, transactional write operations
    /// </summary>
    public interface IWriteContext
    {
        // Cleanup operations
        void DeleteOrphanedBeatmaps();
        void CleanReverseOrphans();

        // Set operations
        void DeleteSet(Guid id);
        void UpdateSetStatus(Guid id, Status newStatus);
        void UpdateSetDates(Guid id, DateTimeOffset? rankedDate, DateTimeOffset? submittedDate);

        // Statistics
        int GetModifiedCount();
    }
}
