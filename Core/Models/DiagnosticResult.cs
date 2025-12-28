namespace RealmPeek.Core.Models
{
    /// <summary>
    /// Results from database diagnostic scan
    /// </summary>
    public class DiagnosticResult
    {
        public int OrphanedBeatmaps { get; set; }
        public int ReverseOrphanSets { get; set; }
        public int DeadReferences { get; set; }

        public bool HasIssues => OrphanedBeatmaps > 0 || ReverseOrphanSets > 0 || DeadReferences > 0;

        public void Print()
        {
            if (OrphanedBeatmaps > 0)
                Console.WriteLine($"Found {OrphanedBeatmaps} orphaned beatmaps (will be cleaned up during execution).");

            if (ReverseOrphanSets > 0 || DeadReferences > 0)
                Console.WriteLine($"Found {ReverseOrphanSets} empty sets and {DeadReferences} dead beatmap references (will be cleaned up during execution).");
        }
    }
}
