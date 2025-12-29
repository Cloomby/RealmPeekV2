using System;
using RealmPeek.Core.Models;
using RealmPeek.Core.Schema;
using Realms;

namespace RealmPeek.Core.Data
{
    /// <summary>
    /// Concrete implementation of Realm repository
    /// </summary>
    public class RealmRepository : IRealmRepository
    {
        private readonly Realm _realm;

        public RealmRepository(string path)
        {
            // Don't use IsReadOnly - causes migration issues
            var config = new RealmConfiguration(path) { SchemaVersion = 51 };
            _realm = Realm.GetInstance(config);
        }

        public List<BeatmapSetModel> LoadAllSets()
        {
            return _realm.All<BeatmapSet>().ToList().Select(MapSetToModel).ToList();
        }

        public BeatmapSetModel? FindSetByID(Guid id)
        {
            var set = _realm.Find<BeatmapSet>(id);
            return set != null ? MapSetToModel(set) : null;
        }

        //public BeatmapSetModel? FindSetByQuery(Guid id)
        //{
        //    var set = _realm.All<BeatmapSet>();
        //    return set != null ? MapSetToModel(set) : null;
        //}

        public BeatmapModel? FindBeatmapByQuery(string query, params QueryArgument[] args)
        {
            Beatmap beatmap;
            try
            {
                beatmap = _realm.All<Beatmap>().Filter(query, args).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FindBeatmapByQuery failed! Error: {ex.Message}");
                return null;
            }
            return beatmap != null ? MapBeatmapToModel(beatmap) : null;
        }

        public DiagnosticResult RunDiagnostics()
        {
            var result = new DiagnosticResult();

            // Count orphaned beatmaps
            result.OrphanedBeatmaps = _realm.All<Beatmap>().ToList().Count(b => b.BeatmapSet == null);

            // Count reverse orphans
            var allSets = _realm.All<BeatmapSet>().ToList();
            var allBeatmapIds = new HashSet<Guid>(_realm.All<Beatmap>().ToList().Select(b => b.ID));

            foreach (var set in allSets)
            {
                var deadMaps = set.Beatmaps.Where(b => !allBeatmapIds.Contains(b.ID)).ToList();
                if (deadMaps.Count > 0)
                {
                    result.DeadReferences += deadMaps.Count;
                    if (deadMaps.Count == set.Beatmaps.Count)
                        result.ReverseOrphanSets++;
                }
            }

            return result;
        }

        public void ExecuteWrite(Action<IWriteContext> action)
        {
            _realm.Write(() => action(new WriteContext(_realm)));
        }

        public void Dispose()
        {
            _realm?.Dispose();
        }

        // Map Realm objects to domain models
        private static BeatmapSetModel MapSetToModel(BeatmapSet BeatmapSet)
        {
            var firstMap = BeatmapSet.Beatmaps.FirstOrDefault();
            var metadata = firstMap?.Metadata;

            return new BeatmapSetModel
            {
                ID = BeatmapSet.ID,
                OnlineID = BeatmapSet.OnlineID,
                Status = (Status)BeatmapSet.Status,
                Title = metadata?.Title ?? "[Unknown]",
                Artist = metadata?.Artist ?? "[Unknown]",
                Creator = metadata?.Author?.Username ?? "[Unknown]",
                DateRanked = BeatmapSet.DateRanked,
                DateSubmitted = BeatmapSet.DateSubmitted,
                DateAdded = BeatmapSet.DateAdded,
                Maps = BeatmapSet
                    .Beatmaps.Select(beatmap =>
                    {
                        return MapBeatmapToModel(beatmap);
                    })
                    .ToList(),
            };
        }

        // Map Realm objects to domain models
        private static BeatmapModel MapBeatmapToModel(Beatmap beatmap)
        {
            return new BeatmapModel
            {
                ID = beatmap.ID,
                DifficultyName = beatmap.DifficultyName ?? "",
                Ruleset = beatmap.Ruleset,
                Difficulty = beatmap.Difficulty,
                Metadata = beatmap.Metadata,
                UserSettings = beatmap.UserSettings,
                BeatmapSet = beatmap.BeatmapSet,
                Status = beatmap.Status,
                OnlineID = beatmap.OnlineID,
                Length = beatmap.Length,
                BPM = beatmap.BPM,
                Hash = beatmap.Hash,
                StarRating = beatmap.StarRating,
                MD5Hash = beatmap.MD5Hash ?? "",
                OnlineMD5Hash = beatmap.OnlineMD5Hash ?? "",
                LastLocalUpdate = beatmap.LastLocalUpdate,
                LastOnlineUpdate = beatmap.LastOnlineUpdate,
                Hidden = beatmap.Hidden,
                TotalObjectCount = beatmap.TotalObjectCount,
                LastPlayed = beatmap.LastPlayed,
                BeatDivisor = beatmap.BeatDivisor,
                EditorTimestamp = beatmap.EditorTimestamp,
            };
        }
    }

    /// <summary>
    /// Internal write context implementation
    /// </summary>
    internal class WriteContext : IWriteContext
    {
        private readonly Realm _realm;
        private int _modifiedCount = 0;

        public WriteContext(Realm realm)
        {
            _realm = realm;
        }

        public void DeleteOrphanedBeatmaps()
        {
            var orphans = _realm.All<Beatmap>().Where(b => b.BeatmapSet == null).ToList();
            foreach (var orphan in orphans)
            {
                _realm.Remove(orphan);
                _modifiedCount++;
            }
        }

        public void CleanReverseOrphans()
        {
            var allSets = _realm.All<BeatmapSet>().ToList();
            var allBeatmapIds = new HashSet<Guid>(_realm.All<Beatmap>().ToList().Select(b => b.ID));

            foreach (var set in allSets)
            {
                var deadMaps = set.Beatmaps.Where(b => !allBeatmapIds.Contains(b.ID)).ToList();

                if (deadMaps.Count > 0)
                {
                    if (deadMaps.Count == set.Beatmaps.Count)
                    {
                        // All beatmaps are dead - delete entire set
                        _realm.Remove(set);
                    }
                    else
                    {
                        // Remove only dead beatmap references
                        foreach (var deadMap in deadMaps)
                            set.Beatmaps.Remove(deadMap);
                    }
                    _modifiedCount += deadMaps.Count;
                }
            }
        }

        public void DeleteSet(Guid id)
        {
            var set = _realm.Find<BeatmapSet>(id);
            if (set != null)
            {
                _realm.Remove(set);
                _modifiedCount++;
            }
        }

        public void DeleteBeatmap(Guid id)
        {
            var beatmap = _realm.Find<Beatmap>(id);
            if (beatmap != null)
            {
                _realm.Remove(beatmap);
                _modifiedCount++;
            }
        }

        public void UpdateSetStatus(Guid id, Status newStatus)
        {
            var set = _realm.Find<BeatmapSet>(id);
            if (set != null)
            {
                set.Status = (int)newStatus;
                foreach (var map in set.Beatmaps)
                    map.Status = (int)newStatus;
                _modifiedCount++;
            }
        }

        public void UpdateSetDates(Guid id, DateTimeOffset? rankedDate, DateTimeOffset? submittedDate)
        {
            var set = _realm.Find<BeatmapSet>(id);
            if (set != null)
            {
                if (rankedDate.HasValue && !set.DateRanked.HasValue)
                    set.DateRanked = rankedDate.Value;

                if (submittedDate.HasValue && !set.DateSubmitted.HasValue)
                    set.DateSubmitted = submittedDate.Value;

                _modifiedCount++;
            }
        }

        public int GetModifiedCount() => _modifiedCount;
    }
}
