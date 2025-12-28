using RealmPeek.Core.Data;
using RealmPeek.Core.Infrastructure;
using RealmPeek.Core.Models;

namespace RealmPeek.Core.Services
{
    /// <summary>
    /// Executes planned changes to the database
    /// </summary>
    public class ExecutionService
    {
        public void Execute(string inputPath, string outputPath, ActionPlan plan)
        {
            Console.WriteLine($"\nCopying database: {Path.GetFileName(inputPath)} → {Path.GetFileName(outputPath)}");
            File.Copy(inputPath, outputPath, overwrite: true);

            Console.WriteLine("Opening database for modifications...");
            using var repo = new RealmRepository(outputPath, readOnly: false);

            repo.ExecuteWrite(ctx =>
            {
                Console.WriteLine("Cleaning orphaned beatmaps...");
                ctx.DeleteOrphanedBeatmaps();

                Console.WriteLine("Cleaning reverse orphans...");
                ctx.CleanReverseOrphans();

                if (plan.ToDelete.Count > 0)
                {
                    Console.WriteLine($"Deleting {plan.ToDelete.Count} ghost/broken sets...");
                    foreach (var set in plan.ToDelete)
                        ctx.DeleteSet(set.ID);
                }

                if (plan.ToUpdate.Count > 0)
                {
                    Console.WriteLine($"Deleting {plan.ToUpdate.Count} outdated sets (ready for re-download)...");
                    foreach (var set in plan.ToUpdate)
                        ctx.DeleteSet(set.ID);
                }

                if (plan.StatusFixes.Count > 0)
                {
                    Console.WriteLine($"Fixing {plan.StatusFixes.Count} status mismatches...");
                    foreach (var fix in plan.StatusFixes)
                        ctx.UpdateSetStatus(fix.Set.ID, fix.NewStatus);
                }

                if (plan.DateBackfills.Count > 0)
                {
                    Console.WriteLine($"Backfilling {plan.DateBackfills.Count} date entries...");
                    foreach (var backfill in plan.DateBackfills)
                        ctx.UpdateSetDates(backfill.Set.ID, backfill.RankedDate, backfill.SubmittedDate);
                }

                if (plan.LocalMapsNeedingDates.Count > 0)
                {
                    Console.WriteLine($"Setting dummy dates for {plan.LocalMapsNeedingDates.Count} local maps...");
                    var dummyDate = DateTimeOffset.Now;
                    foreach (var set in plan.LocalMapsNeedingDates)
                        ctx.UpdateSetDates(set.ID, dummyDate, dummyDate);
                }

                Console.WriteLine($"\n✅ Total modifications: {ctx.GetModifiedCount()}");
            });
        }

        public List<int> GenerateDownloadList(ActionPlan plan, string downloadResultFile)
        {
            var idsToDownload = plan.ToUpdate.Where(s => s.OnlineID > 0).Select(s => s.OnlineID).Distinct().OrderBy(id => id).ToList();

            if (idsToDownload.Count > 0)
            {
                var lines = idsToDownload.Select(id => $"https://osu.ppy.sh/beatmapsets/{id}");
                FilePathHelper.WriteAllLines(downloadResultFile, lines);
                Console.WriteLine($"\n📋 {idsToDownload.Count} beatmaps saved to {downloadResultFile}");
            }

            return idsToDownload;
        }
    }
}
