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
            return _realm.All<BeatmapSet>().ToList().Select(MapToModel).ToList();
        }

        public BeatmapSetModel? FindSet(Guid id)
        {
            var set = _realm.Find<BeatmapSet>(id);
            return set != null ? MapToModel(set) : null;
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
        private static BeatmapSetModel MapToModel(BeatmapSet set)
        {
            var firstMap = set.Beatmaps.FirstOrDefault();
            var metadata = firstMap?.Metadata;

            return new BeatmapSetModel
            {
                ID = set.ID,
                OnlineID = set.OnlineID,
                Status = (Status)set.Status,
                Title = metadata?.Title ?? "[Unknown]",
                Artist = metadata?.Artist ?? "[Unknown]",
                Creator = metadata?.Author?.Username ?? "[Unknown]",
                DateRanked = set.DateRanked,
                DateSubmitted = set.DateSubmitted,
                DateAdded = set.DateAdded,
                Maps = set
                    .Beatmaps.Select(m => new BeatmapModel
                    {
                        ID = m.ID,
                        OnlineID = m.OnlineID,
                        DifficultyName = m.DifficultyName ?? "",
                        MD5Hash = m.MD5Hash ?? "",
                        StarRating = m.StarRating,
                        Length = m.Length,
                        BPM = m.BPM,
                    })
                    .ToList(),
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
