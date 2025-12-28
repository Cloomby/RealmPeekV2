using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RealmPeek
{
    public class NerinyanError
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestID { get; set; }

        // The API nests the Set info INSIDE the Map info

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    public class Downloader
    {
        // SWITCHED MIRROR: Nerinyan (Fresh rate limits!)

        // nv=1 means "No Video"

        private const string MIRROR_URL = "https://api.nerinyan.moe/d/{0}?nv=1";

        private const string DOWNLOAD_DIR = @".\data\downloads";

        private const string FAILED_DOWNLOADS_FILE = @".\data\failed_downloads.txt";

        // TURBO MODE

        private const int CONCURRENCY = 3;

        private const int DELAY_MS = 500; // 0.5s delay

        private const int MAX_RETRIES = 3;

        private readonly HttpClient _client;

        public Downloader()
        {
            _client = new HttpClient();

            _client.Timeout = TimeSpan.FromMinutes(10);

            _client.DefaultRequestHeaders.Add("User-Agent", "RealmPeek/Turbo");
        }

        public async Task DownloadSetsAsync(List<int> setIds)
        {
            if (setIds.Count == 0)
            {
                return;
            }
            Directory.CreateDirectory(GetFullPath(DOWNLOAD_DIR));

            Console.WriteLine($"\n--- STARTING DOWNLOADS ({setIds.Count} sets) ---");

            Console.WriteLine($"Target: {GetFullPath(DOWNLOAD_DIR)}");

            Console.WriteLine("Source: Nerinyan (Turbo Mode)");

            var semaphore = new SemaphoreSlim(CONCURRENCY);

            var tasks = new List<Task>();

            var completed = 0;

            var total = setIds.Count;

            var progressLock = new object();

            var failedDownloads = new ConcurrentBag<int>(); // Track failed set IDs

            foreach (var id in setIds)
            {
                await semaphore.WaitAsync();

                await Task.Delay(DELAY_MS); // Brief courtesy wait

                tasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            var filePath = GetFullPath(Path.Combine(DOWNLOAD_DIR, $"{id}.osz"));

                            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                            {
                                return; // Skip existing
                            }

                            var success = false;

                            var attempts = 0;

                            while (!success && attempts < MAX_RETRIES)
                            {
                                attempts++;

                                try
                                {
                                    var url = string.Format(MIRROR_URL, id);

                                    using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                                    {
                                        if (response.IsSuccessStatusCode)
                                        {
                                            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                            {
                                                await response.Content.CopyToAsync(fs);

                                                Console.WriteLine($"[Sucess] Successfully downloaded {id}!");
                                            }

                                            success = true;
                                        }
                                        else if ((int)response.StatusCode == 429)
                                        {
                                            // If we hit limits again, wait 10s and retry

                                            lock (progressLock)
                                            {
                                                Console.WriteLine($"\n[Busy {response.StatusCode}] Set {id}: Waiting 10s...");
                                            }
                                            await Task.Delay(10000);
                                        }
                                        else if ((int)response.StatusCode == 500)
                                        {
                                            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                                            var error = json.GetProperty("error").GetString() ?? "";

                                            if (error.Contains("set id & map id not found"))
                                            {
                                                Console.WriteLine($"[Failed] Nerinyan does not have the set or map");

                                                failedDownloads.Add(id); // Track for deletion

                                                break; // Don't retry if it doesn't exist
                                            }
                                        }
                                        else
                                        {
                                            lock (progressLock)
                                            {
                                                Console.WriteLine($"\n[Failed] Set {id}: {response.StatusCode}");
                                            }
                                            break;
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    await Task.Delay(2000);
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();

                            var c = Interlocked.Increment(ref completed);

                            lock (progressLock)
                            {
                                Console.Write($"\rDownloading: {c} / {total} ({(double)c / total:P0})    ");
                            }
                        }
                    })
                );
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"\nDownloads Finished.");

            Console.WriteLine($"  Successful: {total - failedDownloads.Count}");

            Console.WriteLine($"  Failed:     {failedDownloads.Count}");

            // Write failed downloads to file for deletion on next run

            if (failedDownloads.Count > 0)
            {
                var failedPath = GetFullPath(FAILED_DOWNLOADS_FILE);

                File.WriteAllLines(failedPath, failedDownloads.Select(id => id.ToString()));

                Console.WriteLine($"\n⚠️  Failed downloads saved to: {FAILED_DOWNLOADS_FILE}");

                Console.WriteLine("    These sets will be marked for deletion on next scan.");
            }
        }

        private string GetFullPath(string relative)
        {
            var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

            return Path.GetFullPath(Path.Combine(baseDir, relative));
        }
    }
}
