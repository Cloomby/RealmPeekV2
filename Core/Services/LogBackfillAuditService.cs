using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RealmPeek.Core.API;
using RealmPeek.Core.Data;
using RealmPeek.Core.Infrastructure;
using RealmPeek.Core.Models;

namespace RealmPeek.Core.Services
{
    public partial class LogBackfillAuditService
    {
        // Lightweight record to hold structured data
        public record MapEntry(string Title, string Artist, string? Author);

        // Result of checking a beatmap
        public record CheckResult(MapEntry Entry, BeatmapModel? Beatmap, BeatmapSetModel? BeatmapSet);

        // Result of API verification
        public record ApiVerification(CheckResult Check, bool ExistsOnline);

        [GeneratedRegex(@"Could not find (?<Artist>.+?) - (?<Title>.+?)(?: \((?<Author>[^)]+)\))? in local cache")]
        private static partial Regex BackfillRegex();

        public List<MapEntry> ParseLogFile(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                Console.WriteLine("No logfile provided!");
                return [];
            }

            if (!File.Exists(logFilePath))
            {
                Console.WriteLine($"Provided logfile not found: {logFilePath}");
                return [];
            }

            List<MapEntry> results = new List<MapEntry>();
            var regex = BackfillRegex();

            // Stream file for better memory usage
            foreach (var line in File.ReadLines(logFilePath))
            {
                // Skip lines that don't match context
                if (!line.Contains("in local cache"))
                    continue;

                var match = regex.Match(line);
                if (match.Success)
                {
                    results.Add(new MapEntry(Title: match.Groups["Title"].Value, Artist: match.Groups["Artist"].Value, Author: match.Groups["Author"].Success ? match.Groups["Author"].Value : null));
                }
            }

            return results;
        }

        public async Task StartBackfillAudit(string logFilePath, string realmFilePath)
        {
            Console.WriteLine("\n--- BACKFILL AUDIT ---");

            // Parse log file
            List<MapEntry> entries = ParseLogFile(logFilePath);
            if (entries.Count == 0)
            {
                Console.WriteLine("No entries found in log file.");
                return;
            }

            Console.WriteLine($"Found {entries.Count} beatmap entries in log.\n");

            var realmPath = FilePathHelper.GetFullPath(realmFilePath);

            // Check which beatmaps exist in database
            var checkResults = new List<CheckResult>();
            using (var repo = new RealmRepository(realmPath))
            {
                Console.WriteLine("Checking database for matching beatmaps...");
                foreach (var entry in entries)
                {
                    // Find beatmap by metadata
                    var beatmap = repo.FindBeatmapByQuery("(Metadata.Title == $0 AND Metadata.Artist == $1) OR Metadata.Author.Username == $2", entry.Title, entry.Artist, entry.Author);

                    BeatmapSetModel? beatmapSet = null;
                    if (beatmap != null && beatmap.BeatmapSet != null)
                    {
                        // ✅ FIXED: Get the set ID from the Realm object
                        beatmapSet = repo.FindSetByID(beatmap.BeatmapSet.ID);
                    }

                    checkResults.Add(new CheckResult(entry, beatmap, beatmapSet));
                }
            }

            // Categorize results
            var found = checkResults.Where(r => r.Beatmap != null).ToList();
            var notFound = checkResults.Where(r => r.Beatmap == null).ToList();

            Console.WriteLine($"\n✅ Found in database: {found.Count}");
            Console.WriteLine($"❌ Not found:         {notFound.Count}");

            if (found.Count == 0)
            {
                Console.WriteLine("\nNo beatmaps to process.");
                return;
            }

            // ✅ DIAGNOSTIC: Check how many have sets
            var withSets = found.Count(r => r.BeatmapSet != null);
            Console.WriteLine($"   With BeatmapSet:    {withSets}");
            Console.WriteLine($"   Without BeatmapSet: {found.Count - withSets}");

            // Show summary
            PrintFoundBeatmaps(found);

            // Prompt for action
            Console.WriteLine("\n--- SELECT ACTION ---");
            Console.WriteLine("  [1] REDOWNLOAD   - Verify with API & queue valid beatmaps for re-download");
            Console.WriteLine("  [2] DUMMY FIX    - Set dummy dates to suppress error");
            Console.WriteLine("  [3] DELETE       - Delete these beatmaps from database");
            Console.WriteLine("  [Q] QUIT         - Exit without changes");
            Console.Write("\n> ");

            string? choice = Console.ReadLine()?.Trim().ToUpperInvariant();

            switch (choice)
            {
                case "1":
                    await HandleRedownloadWithVerification(found, realmPath);
                    break;
                case "2":
                    HandleDummyFix(found, realmPath);
                    break;
                case "3":
                    HandleDelete(found, realmPath);
                    break;
                default:
                    Console.WriteLine("Cancelled.");
                    break;
            }
        }

        private void PrintFoundBeatmaps(List<CheckResult> results)
        {
            Console.WriteLine("\n--- FOUND BEATMAPS ---");
            foreach (var result in results.Take(20))
            {
                var set = result.BeatmapSet;
                var hasOnlineID = set?.OnlineID > 0;
                var idDisplay = hasOnlineID ? $"[SetID: {set.OnlineID}]" : "[No OnlineID]";

                Console.WriteLine($"  {result.Entry.Artist} - {result.Entry.Title} ({result.Entry.Author}) {idDisplay}");
            }

            if (results.Count > 20)
                Console.WriteLine($"  ... and {results.Count - 20} more");
        }

        private async Task HandleRedownloadWithVerification(List<CheckResult> results, string realmPath)
        {
            Console.WriteLine("\n--- REDOWNLOAD MODE (API VERIFICATION) ---");

            // Separate by whether they have OnlineID
            var withOnlineID = results.Where(r => r.BeatmapSet?.OnlineID > 0).ToList();
            var noOnlineID = results.Where(r => r.BeatmapSet?.OnlineID <= 0).ToList();

            Console.WriteLine($"Beatmaps with OnlineID:    {withOnlineID.Count}");
            Console.WriteLine($"Beatmaps without OnlineID: {noOnlineID.Count}");

            if (withOnlineID.Count == 0)
            {
                Console.WriteLine("\n⚠️ No beatmaps have valid OnlineIDs. Cannot verify or redownload.");
                return;
            }

            // Get first valid map ID from each set to query API
            var mapIdsToQuery = new List<int>();
            var mapIdToResult = new Dictionary<int, CheckResult>();

            foreach (var result in withOnlineID.DistinctBy(r => r.BeatmapSet!.OnlineID))
            {
                // Get first map with valid OnlineID
                var firstValidMapID = result.BeatmapSet!.Maps.FirstOrDefault(m => m.OnlineID > 0)?.OnlineID ?? 0;

                if (firstValidMapID > 0)
                {
                    mapIdsToQuery.Add(firstValidMapID);
                    mapIdToResult[firstValidMapID] = result;
                }
            }

            if (mapIdsToQuery.Count == 0)
            {
                Console.WriteLine("\n⚠️ No valid map IDs found. Cannot query API.");
                return;
            }

            // Verify with API
            Console.WriteLine($"\nVerifying {mapIdsToQuery.Count} beatmaps with osu! API...");
            var api = new OsuApiAdapter();

            if (!await api.AuthenticateAsync())
            {
                Console.WriteLine("❌ Authentication failed! Cannot verify.");
                return;
            }

            var apiResults = await api.GetBeatmapsAsync(mapIdsToQuery);

            // Categorize results
            var validSets = new List<BeatmapSetModel>();
            var deadSets = new List<CheckResult>();

            foreach (var apiResult in apiResults)
            {
                if (!mapIdToResult.TryGetValue(apiResult.Id, out var checkResult))
                    continue;

                if (apiResult.IsValid && apiResult.BeatmapSet != null)
                {
                    validSets.Add(checkResult.BeatmapSet!);
                }
                else
                {
                    deadSets.Add(checkResult);
                }
            }

            // Print results
            Console.WriteLine($"\n✅ Valid online:  {validSets.Count}");
            Console.WriteLine($"❌ Dead/Missing:  {deadSets.Count}");

            // Save valid downloads
            if (validSets.Count > 0)
            {
                var downloadFile = FilePathHelper.GetFullPath(@".\data\backfill\backfill_downloads.txt");
                FilePathHelper.EnsureDirectoryExists(@".\data\backfill");

                var lines = validSets.DistinctBy(s => s.OnlineID).Select(s => $"https://osu.ppy.sh/beatmapsets/{s.OnlineID}");
                File.WriteAllLines(downloadFile, lines);

                Console.WriteLine($"\n✅ Saved {validSets.Count} valid beatmap URLs to: backfill_downloads.txt");
                Console.WriteLine("   Use DOWNLOAD mode or manually download these beatmaps.");
            }

            // Handle dead beatmaps
            if (deadSets.Count > 0)
            {
                Console.WriteLine($"\n⚠️ {deadSets.Count} beatmaps do not exist online.");
                Console.WriteLine("   What should we do with them?");
                Console.WriteLine("   [1] DELETE    - Remove from database");
                Console.WriteLine("   [2] DUMMY FIX - Set dummy dates to suppress error");
                Console.WriteLine("   [Q] SKIP      - Leave as is");
                Console.Write("\n> ");

                string? deadChoice = Console.ReadLine()?.Trim().ToUpperInvariant();

                switch (deadChoice)
                {
                    case "1":
                        HandleDelete(deadSets, realmPath);
                        break;
                    case "2":
                        HandleDummyFix(deadSets, realmPath);
                        break;
                    default:
                        Console.WriteLine("Skipped dead beatmaps.");
                        break;
                }
            }

            // Handle no OnlineID beatmaps
            if (noOnlineID.Count > 0)
            {
                Console.WriteLine($"\n⚠️ {noOnlineID.Count} beatmaps have no OnlineID and cannot be verified.");
                Console.WriteLine("   What should we do with them?");
                Console.WriteLine("   [1] DELETE    - Remove from database");
                Console.WriteLine("   [2] DUMMY FIX - Set dummy dates to suppress error");
                Console.WriteLine("   [Q] SKIP      - Leave as is");
                Console.Write("\n> ");

                string? noIdChoice = Console.ReadLine()?.Trim().ToUpperInvariant();

                switch (noIdChoice)
                {
                    case "1":
                        HandleDelete(noOnlineID, realmPath);
                        break;
                    case "2":
                        HandleDummyFix(noOnlineID, realmPath);
                        break;
                    default:
                        Console.WriteLine("Skipped beatmaps without OnlineID.");
                        break;
                }
            }
        }

        private void HandleDummyFix(List<CheckResult> results, string realmPath)
        {
            Console.WriteLine("\n--- DUMMY FIX MODE ---");
            Console.WriteLine($"This will set dummy dates for {results.Count} beatmaps to suppress backfill errors.");
            Console.Write("Continue? (y/n): ");

            if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y")
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            var dummyDate = DateTimeOffset.Now;
            int fixedCount = 0;

            using (var repo = new RealmRepository(realmPath))
            {
                repo.ExecuteWrite(ctx =>
                {
                    foreach (var result in results)
                    {
                        if (result.BeatmapSet != null)
                        {
                            ctx.UpdateSetDates(result.BeatmapSet.ID, dummyDate, dummyDate);
                            fixedCount++;
                        }
                    }
                });
            }

            Console.WriteLine($"\n✅ Applied dummy dates to {fixedCount} beatmap sets.");
        }

        // CORRECTED HandleDelete method for LogBackfillAuditService.cs

        private void HandleDelete(List<CheckResult> results, string realmPath)
        {
            Console.WriteLine("\n--- DELETE MODE ---");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️ WARNING: This will permanently delete {results.Count} beatmaps from your database!");
            Console.ResetColor();
            Console.Write("Type 'DELETE' to confirm: ");

            if (Console.ReadLine()?.Trim() != "DELETE")
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            int deletedSetCount = 0;
            int deletedBeatmapCount = 0;

            using (var repo = new RealmRepository(realmPath))
            {
                repo.ExecuteWrite(ctx =>
                {
                    foreach (var result in results)
                    {
                        // ✅ FIXED: Check both Beatmap and BeatmapSet
                        if (result.Beatmap != null)
                        {
                            // If beatmap has a set, delete the entire set
                            if (result.BeatmapSet != null)
                            {
                                var beatmapCount = result.BeatmapSet.Maps.Count;
                                ctx.DeleteSet(result.BeatmapSet.ID);
                                deletedSetCount++;
                                deletedBeatmapCount += beatmapCount;
                            }
                            else
                            {
                                // Orphaned beatmap - delete just the beatmap
                                ctx.DeleteBeatmap(result.Beatmap.ID);
                                deletedBeatmapCount++;
                            }
                        }
                    }
                });
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ Deleted {deletedSetCount} beatmap sets.");
            Console.WriteLine($"✅ Deleted {deletedBeatmapCount} beatmaps.");
            Console.ResetColor();
        }
    }
}
