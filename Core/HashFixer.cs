using RealmPeek.Core.Schema;
using Realms;

namespace RealmPeek
{
    public class HashFixer
    {
        // Copy hashes from corrupted DB to good DB
        public static void FixHashes(string goodRealmPath, string corruptedRealmPath)
        {
            Console.WriteLine("\n--- HASH FIXER ---");
            Console.WriteLine($"Good DB:      {goodRealmPath}");
            Console.WriteLine($"Corrupted DB: {corruptedRealmPath}");

            // Load corrupted DB (read-only to get hashes)
            var corruptedConfig = new RealmConfiguration(corruptedRealmPath) { IsReadOnly = true, SchemaVersion = 51 };

            // Build hash lookup from corrupted DB
            Dictionary<int, List<SetHashInfo>> corruptedHashes;
            using (var corruptedRealm = Realm.GetInstance(corruptedConfig))
            {
                Console.WriteLine("Loading hashes from corrupted database...");
                corruptedHashes = corruptedRealm
                    .All<BeatmapSet>()
                    .ToList()
                    .Where(s => s.OnlineID > 0)
                    .GroupBy(s => s.OnlineID)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                            g.Select(s => new SetHashInfo
                                {
                                    SetHash = s.Hash,
                                    FileHashes = s.Files.Select(f => new FileHashInfo { Filename = f.Filename, Hash = f.File?.Hash }).ToList(),
                                    MapHashes = s
                                        .Beatmaps.Select(m => new MapHashInfo
                                        {
                                            OnlineID = m.OnlineID,
                                            Hash = m.Hash,
                                            MD5Hash = m.MD5Hash,
                                        })
                                        .ToList(),
                                })
                                .ToList()
                    );

                var totalSets = corruptedHashes.Sum(kvp => kvp.Value.Count);
                var duplicates = corruptedHashes.Count(kvp => kvp.Value.Count > 1);
                Console.WriteLine($"Loaded {totalSets} sets from corrupted DB ({duplicates} duplicate OnlineIDs).");
            }

            // Apply to good DB
            var goodConfig = new RealmConfiguration(goodRealmPath) { SchemaVersion = 51 };
            using (var goodRealm = Realm.GetInstance(goodConfig))
            {
                Console.WriteLine("Applying hashes to good database...");

                var setsFixed = 0;
                var mapsFixed = 0;
                var filesFixed = 0;

                goodRealm.Write(() =>
                {
                    var allSets = goodRealm.All<BeatmapSet>().ToList();

                    foreach (var set in allSets)
                    {
                        if (set.OnlineID <= 0)
                        {
                            continue;
                        }

                        // Find matching sets in corrupted DB
                        if (!corruptedHashes.TryGetValue(set.OnlineID, out var corruptedInfoList))
                        {
                            continue;
                        }

                        // Try to find best match - prefer one with matching map count
                        var corruptedInfo = corruptedInfoList.Count == 1 ? corruptedInfoList[0] : corruptedInfoList.FirstOrDefault(c => c.MapHashes.Count == set.Beatmaps.Count) ?? corruptedInfoList[0];

                        if (corruptedInfoList.Count > 1)
                        {
                            Console.WriteLine($"  Note: OnlineID {set.OnlineID} has {corruptedInfoList.Count} duplicates, using best match");
                        }

                        // Fix set hash
                        if (!string.IsNullOrEmpty(corruptedInfo.SetHash) && set.Hash != corruptedInfo.SetHash)
                        {
                            set.Hash = corruptedInfo.SetHash;
                            setsFixed++;
                        }

                        // Fix file hashes
                        foreach (var corruptedFile in corruptedInfo.FileHashes)
                        {
                            var matchingFile = set.Files.FirstOrDefault(f => f.Filename == corruptedFile.Filename);
                            if (matchingFile != null && !string.IsNullOrEmpty(corruptedFile.Hash))
                            {
                                if (matchingFile.File == null)
                                {
                                    // Create File object if missing
                                    var existingFile = goodRealm.Find<Core.Schema.File>(corruptedFile.Hash);
                                    if (existingFile == null)
                                    {
                                        existingFile = goodRealm.Add(new Core.Schema.File { Hash = corruptedFile.Hash });
                                    }
                                    matchingFile.File = existingFile;
                                    filesFixed++;
                                }
                                else if (matchingFile.File.Hash != corruptedFile.Hash)
                                {
                                    // Update existing File
                                    var existingFile = goodRealm.Find<Core.Schema.File>(corruptedFile.Hash);
                                    if (existingFile == null)
                                    {
                                        existingFile = goodRealm.Add(new Core.Schema.File { Hash = corruptedFile.Hash });
                                    }
                                    matchingFile.File = existingFile;
                                    filesFixed++;
                                }
                            }
                        }

                        // Fix beatmap hashes
                        foreach (var corruptedMap in corruptedInfo.MapHashes)
                        {
                            var matchingMap = set.Beatmaps.FirstOrDefault(m => m.OnlineID == corruptedMap.OnlineID);
                            if (matchingMap != null)
                            {
                                var mapChanged = false;

                                if (!string.IsNullOrEmpty(corruptedMap.Hash) && matchingMap.Hash != corruptedMap.Hash)
                                {
                                    matchingMap.Hash = corruptedMap.Hash;
                                    mapChanged = true;
                                }

                                if (!string.IsNullOrEmpty(corruptedMap.MD5Hash) && matchingMap.MD5Hash != corruptedMap.MD5Hash)
                                {
                                    matchingMap.MD5Hash = corruptedMap.MD5Hash;
                                    mapChanged = true;
                                }

                                if (mapChanged)
                                {
                                    mapsFixed++;
                                }
                            }
                        }
                    }
                });

                Console.WriteLine($"\n✅ Hash fixing complete!");
                Console.WriteLine($"   Sets fixed:  {setsFixed}");
                Console.WriteLine($"   Maps fixed:  {mapsFixed}");
                Console.WriteLine($"   Files fixed: {filesFixed}");
            }
        }
    }

    // Helper classes for hash storage
    class SetHashInfo
    {
        public string? SetHash { get; set; }
        public List<FileHashInfo> FileHashes { get; set; } = new List<FileHashInfo>();
        public List<MapHashInfo> MapHashes { get; set; } = new List<MapHashInfo>();
    }

    class FileHashInfo
    {
        public string? Filename { get; set; }
        public string? Hash { get; set; }
    }

    class MapHashInfo
    {
        public int OnlineID { get; set; }
        public string? Hash { get; set; }
        public string? MD5Hash { get; set; }
    }
}
