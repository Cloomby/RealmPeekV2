using System.Collections.Concurrent;

namespace RealmPeek.Core.Models
{
    /// <summary>
    /// Contains all planned database changes from an audit
    /// </summary>
    public class ActionPlan
    {
        public ConcurrentBag<BeatmapSetModel> ToDelete { get; set; } = new();
        public ConcurrentBag<BeatmapSetModel> ToUpdate { get; set; } = new();
        public ConcurrentBag<StatusFix> StatusFixes { get; set; } = new();
        public ConcurrentBag<DateBackfill> DateBackfills { get; set; } = new();
        public ConcurrentBag<BeatmapSetModel> LocalMapsNeedingDates { get; set; } = new();

        public bool IsEmpty =>
            ToDelete.IsEmpty &&
            ToUpdate.IsEmpty &&
            StatusFixes.IsEmpty &&
            DateBackfills.IsEmpty &&
            LocalMapsNeedingDates.IsEmpty;

        public int TotalActions =>
            ToDelete.Count +
            ToUpdate.Count +
            StatusFixes.Count +
            DateBackfills.Count +
            LocalMapsNeedingDates.Count;

        public void PrintSummary(int orphanCount, int reverseOrphanSets, int deadRefs)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- ACTION PLAN ---");
            Console.ResetColor();

            if (orphanCount > 0 || reverseOrphanSets > 0)
            {
                Console.WriteLine($"[ORPHANS]  Orphaned Beatmaps:      {orphanCount}");
                Console.WriteLine($"[REVERSE]  Reverse Orphans:        {reverseOrphanSets} sets, {deadRefs} dead refs");
            }

            Console.WriteLine($"[DELETE]   Broken/Ghost Maps:      {ToDelete.Count}");
            Console.WriteLine($"[UPDATE]   Outdated Maps:          {ToUpdate.Count}");
            Console.WriteLine($"[FIX]      Status Fixes:           {StatusFixes.Count}");
            Console.WriteLine($"[BACKFILL] Date Backfills:         {DateBackfills.Count}");
            Console.WriteLine($"[LOCAL]    Local Maps (Dates):     {LocalMapsNeedingDates.Count}");
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Total Actions: {TotalActions + orphanCount + reverseOrphanSets}");
        }
    }

    /// <summary>
    /// Represents a status fix to be applied
    /// </summary>
    public record StatusFix(BeatmapSetModel Set, Status NewStatus);

    /// <summary>
    /// Represents date backfill to be applied
    /// </summary>
    public record DateBackfill(
        BeatmapSetModel Set,
        DateTimeOffset? RankedDate,
        DateTimeOffset? SubmittedDate
    );
}
