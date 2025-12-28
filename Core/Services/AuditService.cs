using RealmPeek.Core.API;
using RealmPeek.Core.Infrastructure;
using RealmPeek.Core.Models;
using RealmPeek.Core.Schema;

namespace RealmPeek.Core.Services
{
    /// <summary>
    /// Audits beatmap sets against osu! API and plans fixes
    /// </summary>
    public class AuditService
    {
        private readonly OsuApiAdapter _api;
        private readonly ApiCache _cache;
        private readonly HashSet<int> _failedDownloads;

        private const int BATCH_SIZE = 50;
        private const int MAX_PARALLELISM = 4;

        public AuditService(OsuApiAdapter api, ApiCache cache, HashSet<int> failedDownloads)
        {
            _api = api;
            _cache = cache;
            _failedDownloads = failedDownloads;
        }

        public async Task<ActionPlan> AuditAsync(List<BeatmapSetModel> sets)
        {
            var plan = new ActionPlan();
            int processedCount = 0;
            int batchesSinceSave = 0;
            int total = sets.Count;

            Console.WriteLine($"Scanning {sets.Count} sets with {MAX_PARALLELISM}x Concurrency...");

            // Process in batches with parallelism
            await Parallel.ForEachAsync(
                sets.Chunk(BATCH_SIZE),
                new ParallelOptions { MaxDegreeOfParallelism = MAX_PARALLELISM },
                async (batch, _) =>
                {
                    await ProcessBatch(batch.ToList(), plan);

                    // Save cache periodically
                    if (Interlocked.Increment(ref batchesSinceSave) >= 10)
                    {
                        Interlocked.Exchange(ref batchesSinceSave, 0);
                        _cache.Save();
                    }

                    // Update progress
                    int current = Interlocked.Add(ref processedCount, batch.Length);
                    if (current % 100 == 0 || current == total)
                        Console.Write($"\rProgress: {current} / {total} ({(double)current / total:P0})   ");
                }
            );

            _cache.Save();
            Console.WriteLine("\nScan Complete.");

            return plan;
        }

        private async Task ProcessBatch(List<BeatmapSetModel> batch, ActionPlan plan)
        {
            // Step 1: Collect IDs to query
            var mapIdsNeeded = new List<int>();
            var mapIdToSet = new Dictionary<int, BeatmapSetModel>();

            foreach (var set in batch)
            {
                // Check failed downloads first
                if (_failedDownloads.Contains(set.OnlineID))
                {
                    plan.ToDelete.Add(set);
                    continue;
                }

                int validId = set.FirstValidMapID;
                if (validId > 0)
                {
                    if (!mapIdToSet.ContainsKey(validId))
                    {
                        mapIdToSet.Add(validId, set);
                        if (!_cache.Contains(validId))
                            mapIdsNeeded.Add(validId);
                    }
                }
            }

            // Step 2: Fetch from API
            if (mapIdsNeeded.Any())
            {
                var results = await _api.GetBeatmapsAsync(mapIdsNeeded);
                foreach (var result in results)
                    _cache.Add(result.Id, result);

                // Cache invalid results (404s)
                foreach (var neededId in mapIdsNeeded.Where(id => !_cache.Contains(id)))
                {
                    var dummy = new ApiBeatmap { Id = neededId, BeatmapSet = null };
                    _cache.Add(neededId, dummy);
                }
            }

            // Step 3: Process logic
            foreach (var set in batch)
            {
                if (_failedDownloads.Contains(set.OnlineID))
                    continue;

                int lookupId = set.FirstValidMapID;

                if (lookupId > 0)
                    ProcessSetWithValidID(set, lookupId, plan);
                else
                    ProcessSetWithoutValidID(set, plan);
            }
        }

        private void ProcessSetWithValidID(BeatmapSetModel set, int lookupId, ActionPlan plan)
        {
            if (!_cache.TryGet(lookupId, out var onlineMap) || onlineMap == null)
            {
                plan.ToDelete.Add(set);
                return;
            }

            // Invalid (doesn't exist online)
            if (onlineMap.BeatmapSet == null)
            {
                plan.ToDelete.Add(set);
                return;
            }

            // Valid - check for fixes
            var onlineStatus = StatusParser.Parse(onlineMap.BeatmapSet.Status ?? "");

            // Status fix
            if (set.Status != onlineStatus)
            {
                Console.WriteLine($"{set.OnlineID} ({set.Title}) {set.Status} => {onlineStatus}");
                plan.StatusFixes.Add(new StatusFix(set, onlineStatus));
            }

            // Date backfill
            bool needsRanked = !set.DateRanked.HasValue && onlineMap.BeatmapSet.RankedDate.HasValue;
            bool needsSubmitted = !set.DateSubmitted.HasValue && onlineMap.BeatmapSet.SubmittedDate.HasValue;

            if (needsRanked || needsSubmitted)
            {
                plan.DateBackfills.Add(new DateBackfill(set, onlineMap.BeatmapSet.RankedDate, onlineMap.BeatmapSet.SubmittedDate));
            }

            // MD5 hash check (outdated)
            var matchingMap = set.Maps.FirstOrDefault(m => m.OnlineID == onlineMap.Id);
            if (matchingMap != null && matchingMap.MD5Hash != onlineMap.MD5Hash)
                plan.ToUpdate.Add(set);
        }

        private void ProcessSetWithoutValidID(BeatmapSetModel set, ActionPlan plan)
        {
            // No valid map IDs to query API with
            if (set.HasZeroStarRatings)
            {
                plan.ToDelete.Add(set);
            }
            else if (set.HasValidOnlineID && set.NeedsDates)
            {
                // Set has OnlineID but maps don't - needs re-download
                plan.ToUpdate.Add(set);
            }
            else if (set.NeedsDates)
            {
                // Local map - set dummy dates
                plan.LocalMapsNeedingDates.Add(set);
            }
        }
    }

    /// <summary>
    /// Parses osu! API status strings to Status enum
    /// </summary>
    public static class StatusParser
    {
        public static Status Parse(string status)
        {
            return status.ToLowerInvariant() switch
            {
                "ranked" => Status.Ranked,
                "approved" => Status.Approved,
                "qualified" => Status.Qualified,
                "loved" => Status.Loved,
                "graveyard" => Status.Graveyard,
                "wip" => Status.WIP,
                "pending" => Status.Pending,
                _ => Status.Unknown,
            };
        }
    }
}
