using dotenv.net;
using RealmPeek.Core.API;
using RealmPeek.Core.Data;
using RealmPeek.Core.Infrastructure;
using RealmPeek.Core.Models;
using RealmPeek.Core.Services;

namespace RealmPeek.CLI
{
    class Program
    {
        const string REALM_FILE = @".\data\realm\client.realm";
        const string OUTPUT_REALM_FILE = @".\data\realm\client_modified.realm";
        const string CACHE_FILE = @".\data\cache\api_cache.json";
        const string DOWNLOAD_RESULT_FILE = @".\data\downloads\downloads.txt";
        const string FAILED_DOWNLOADS_FILE = @".\data\downloads\failed_downloads.txt";

        static async Task Main(string[] args)
        {
            DotEnv.Load();

            string mode = PromptMode();

            switch (mode.ToUpperInvariant())
            {
                case "INSPECT":
                    RunInspectMode();
                    break;

                case "HASH_FIX":
                    RunHashFixMode();
                    break;

                case "DOWNLOAD":
                    await RunDownloadMode();
                    break;

                default:
                    await RunNormalMode();
                    break;
            }
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================");
            Console.WriteLine("  RealmPeek - osu! Database Manager");
            Console.WriteLine("===========================================");
            Console.ResetColor();
        }

        static string PromptMode()
        {
            Console.WriteLine("\nSelect mode:");
            Console.WriteLine("  [ENTER]    - Normal scan & fix");
            Console.WriteLine("  HASH       - Copy hashes from corrupted DB to good DB");
            Console.WriteLine("  DOWNLOAD   - Download only (if downloads.txt exists)");
            Console.WriteLine("  INSPECT    - Inspect a specific beatmap/set by GUID");
            Console.Write("\n> ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        static void RunInspectMode()
        {
            Console.WriteLine("\n--- INSPECT MODE ---");

            Console.Write("Enter realm file path (or ENTER for default): ");
            string path = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(path))
                path = FilePathHelper.GetFullPath(REALM_FILE);

            Console.Write("Enter BeatmapSet GUID (or ENTER to skip): ");
            string setGuid = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Enter Beatmap GUID (or ENTER to skip): ");
            string mapGuid = Console.ReadLine()?.Trim() ?? "";

            InspectorService.Inspect(path, setGuid, mapGuid);
            WaitForExit();
        }

        static void RunHashFixMode()
        {
            Console.WriteLine("\n--- HASH FIX MODE ---");

            Console.Write("Enter GOOD realm path: ");
            string goodPath = Console.ReadLine()?.Trim() ?? FilePathHelper.GetFullPath(REALM_FILE);

            Console.Write("Enter CORRUPTED realm path: ");
            string corruptedPath = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(corruptedPath))
            {
                Console.WriteLine("Corrupted path required. Exiting.");
                WaitForExit();
                return;
            }

            HashFixer.FixHashes(goodPath, corruptedPath);
            WaitForExit();
        }

        static async Task RunDownloadMode()
        {
            string downloadFile = FilePathHelper.GetFullPath(DOWNLOAD_RESULT_FILE);
            if (!File.Exists(downloadFile))
            {
                Console.WriteLine($"File not found: {downloadFile}");
                WaitForExit();
                return;
            }

            var lines = File.ReadAllLines(downloadFile);
            var ids = lines.Select(l => l.Split('/').Last()).Select(s => int.TryParse(s, out int n) ? n : 0).Where(n => n > 0).Distinct().ToList();

            Console.WriteLine($"Parsed {ids.Count} unique Set IDs to download.");

            var downloader = new Downloader();
            await downloader.DownloadSetsAsync(ids);

            WaitForExit();
        }

        static async Task RunNormalMode()
        {
            string realmPath = FilePathHelper.GetFullPath(REALM_FILE);
            string outputPath = FilePathHelper.GetFullPath(OUTPUT_REALM_FILE);

            Console.WriteLine("\n--- PHASE 1: SCANNING DATABASE ---");

            // Load beatmap sets
            List<BeatmapSetModel> sets;
            DiagnosticResult diagnostics;

            using (var repo = new RealmRepository(realmPath))
            {
                sets = repo.LoadAllSets();
                Console.WriteLine($"Loaded {sets.Count} beatmap sets.");

                diagnostics = repo.RunDiagnostics();
                diagnostics.Print();
            }

            // Load failed downloads
            var failedDownloads = LoadFailedDownloads();
            if (failedDownloads.Count > 0)
                Console.WriteLine($"Found {failedDownloads.Count} sets that failed to download (will be marked for deletion).");

            // Authenticate with osu! API
            var api = new OsuApiAdapter();
            var cache = new ApiCache(CACHE_FILE);

            Console.WriteLine("\nLoading API Cache...");
            Console.WriteLine($"Cache contains {cache.Count} maps.");

            Console.WriteLine("Authenticating with osu! API...");
            if (!await api.AuthenticateAsync())
            {
                Console.WriteLine("❌ Authentication failed!");
                WaitForExit();
                return;
            }

            // Audit beatmaps
            var auditService = new AuditService(api, cache, failedDownloads);
            var plan = await auditService.AuditAsync(sets);

            // Print action plan
            plan.PrintSummary(diagnostics.OrphanedBeatmaps, diagnostics.ReverseOrphanSets, diagnostics.DeadReferences);

            if (plan.IsEmpty && !diagnostics.HasIssues)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✅ Your library is perfect! No actions needed.");
                Console.ResetColor();
                WaitForExit();
                return;
            }

            // Prompt for execution
            Console.WriteLine("\nType 'EXECUTE' to apply these changes.");
            Console.WriteLine("Type 'EXECUTE_DL' to apply changes AND download updates automatically.");
            Console.Write("> ");
            string input = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";

            bool autoDownload = input == "EXECUTE_DL";

            if (input != "EXECUTE" && input != "EXECUTE_DL")
            {
                Console.WriteLine("Aborted.");
                WaitForExit();
                return;
            }

            // Execute changes
            Console.WriteLine("\n--- PHASE 2: APPLYING CHANGES ---");
            var executor = new ExecutionService();
            executor.Execute(realmPath, outputPath, plan);

            // Generate download list
            var idsToDownload = executor.GenerateDownloadList(plan, DOWNLOAD_RESULT_FILE);

            // Auto-download if requested
            if (autoDownload && idsToDownload.Count > 0)
            {
                var downloader = new Downloader();
                await downloader.DownloadSetsAsync(idsToDownload);
            }

            // Cleanup failed downloads file
            string failedPath = FilePathHelper.GetFullPath(FAILED_DOWNLOADS_FILE);
            if (File.Exists(failedPath))
                File.Delete(failedPath);

            // Success message
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n--- ✅ DONE ---");
            Console.ResetColor();
            Console.WriteLine($"Modified database saved to: {outputPath}");
            Console.WriteLine("\nNext steps:");
            Console.WriteLine("1. Close osu!lazer if running.");
            Console.WriteLine($"2. Replace your client.realm with: {Path.GetFileName(outputPath)}");
            Console.WriteLine("3. Open osu!lazer Settings → Maintenance → 'Clean up files'.");

            if (autoDownload && idsToDownload.Count > 0)
                Console.WriteLine($"4. Drag the 'data/downloads' folder into osu!lazer to import {idsToDownload.Count} maps.");
            else if (idsToDownload.Count > 0)
                Console.WriteLine($"4. Use 'downloads.txt' to re-download {idsToDownload.Count} updated maps.");

            WaitForExit();
        }

        static HashSet<int> LoadFailedDownloads()
        {
            var lines = FilePathHelper.ReadAllLines(FAILED_DOWNLOADS_FILE);
            var failedIds = new HashSet<int>();

            foreach (var line in lines)
            {
                if (int.TryParse(line.Trim(), out int id))
                    failedIds.Add(id);
            }

            return failedIds;
        }

        static void WaitForExit()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
