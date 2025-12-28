using System.Collections.Concurrent;
using System.Text.Json;
using RealmPeek.Core.API;

namespace RealmPeek.Core.Infrastructure
{
    /// <summary>
    /// Thread-safe cache for osu! API responses
    /// </summary>
    public class ApiCache
    {
        private readonly ConcurrentDictionary<int, BeatmapData> _cache = new();
        private readonly string _cacheFile;
        private readonly object _fileLock = new();

        public ApiCache(string cacheFile)
        {
            _cacheFile = cacheFile;
            Load();
        }

        public int Count => _cache.Count;

        public bool Contains(int id) => _cache.ContainsKey(id);

        public void Add(int id, BeatmapData beatmap) => _cache.TryAdd(id, beatmap);

        public bool TryGet(int id, out BeatmapData? beatmap) => _cache.TryGetValue(id, out beatmap);

        public void Load()
        {
            string cachePath = FilePathHelper.GetFullPath(_cacheFile);
            if (!File.Exists(cachePath))
                return;

            try
            {
                string json = File.ReadAllText(cachePath);
                var items = JsonSerializer.Deserialize<BeatmapData[]>(json);
                if (items != null)
                {
                    foreach (var item in items)
                        _cache.TryAdd(item.Id, item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load cache: {ex.Message}");
                // Continue with empty cache
            }
        }

        public void Save()
        {
            lock (_fileLock)
            {
                try
                {
                    string cachePath = FilePathHelper.GetFullPath(_cacheFile);
                    var options = new JsonSerializerOptions { WriteIndented = false };
                    string json = JsonSerializer.Serialize(_cache.Values.ToArray(), options);
                    File.WriteAllText(cachePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to save cache: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
