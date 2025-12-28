using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OsuSharp;
using OsuSharp.Models;

namespace RealmPeek.Core.API
{
    /// <summary>
    /// Wrapper around OsuSharp library
    /// Adapts OsuSharp to our existing interface
    /// </summary>
    public class OsuApiAdapter : IDisposable
    {
        private readonly OsuClient _client;
        private bool _isAuthenticated;

        public OsuApiAdapter(long clientId, string clientSecret)
        {
            var credentials = new OsuClientConfiguration { ClientId = clientId, ClientSecret = clientSecret };

            _client = new OsuClient(credentials);
        }

        /// <summary>
        /// Authenticate with osu! API
        /// OsuSharp handles token management automatically
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                // OsuSharp authenticates on first API call
                // We can test with a simple request
                await _client.GetUserAsync(2); // peppy's user ID
                _isAuthenticated = true;
                Console.WriteLine("✅ Authenticated with OsuSharp");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Authentication failed: {ex.Message}");
                _isAuthenticated = false;
                return false;
            }
        }

        /// <summary>
        /// Get beatmaps by IDs (bulk query)
        /// OsuSharp limitation: Must query individually, no bulk endpoint
        /// </summary>
        public async Task<List<BeatmapData>> GetBeatmapsAsync(IEnumerable<int> ids)
        {
            if (!_isAuthenticated)
            {
                Console.WriteLine("Not authenticated, attempting to authenticate...");
                if (!await AuthenticateAsync())
                    return new List<BeatmapData>();
            }

            var results = new List<BeatmapData>();
            var validIds = ids.Where(x => x > 0).Distinct().ToList();

            Console.WriteLine($"Fetching {validIds.Count} beatmaps from API...");

            // OsuSharp doesn't have bulk beatmap query
            // We need to fetch individually (slower but reliable)
            foreach (var id in validIds)
            {
                try
                {
                    var beatmap = await _client.GetBeatmapAsync(id);
                    if (beatmap != null)
                    {
                        results.Add(ConvertToOurModel(beatmap));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch beatmap {id}: {ex.Message}");
                    // Add dummy entry for caching
                    results.Add(new BeatmapData { Id = id, IsValid = false });
                }

                // Rate limiting: small delay between requests
                if (validIds.Count > 10)
                    await Task.Delay(100);
            }

            return results;
        }

        /// <summary>
        /// Convert OsuSharp's Beatmap model to our internal model
        /// </summary>
        private BeatmapData ConvertToOurModel(Beatmap beatmap)
        {
            return new BeatmapData
            {
                Id = beatmap.Id,
                IsValid = true,
                MD5Hash = beatmap.Checksum,
                BeatmapSet = new BeatmapSetData
                {
                    Id = beatmap.BeatmapsetId,
                    Status = ConvertStatus(beatmap.Status),
                    LastUpdated = beatmap.LastUpdated,
                    RankedDate = beatmap.Beatmapset?.RankedDate,
                    SubmittedDate = beatmap.Beatmapset?.SubmittedDate,
                    Title = beatmap.Beatmapset?.Title ?? "",
                    Artist = beatmap.Beatmapset?.Artist ?? "",
                    Creator = beatmap.Beatmapset?.Creator ?? "",
                },
            };
        }

        private string ConvertStatus(BeatmapStatus status)
        {
            return status switch
            {
                BeatmapStatus.Ranked => "ranked",
                BeatmapStatus.Approved => "approved",
                BeatmapStatus.Qualified => "qualified",
                BeatmapStatus.Loved => "loved",
                BeatmapStatus.Graveyard => "graveyard",
                BeatmapStatus.WIP => "wip",
                BeatmapStatus.Pending => "pending",
                _ => "unknown",
            };
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    /// <summary>
    /// Our internal beatmap data model
    /// Decoupled from both OsuSharp and our old API models
    /// </summary>
    public class BeatmapData
    {
        public int Id { get; set; }
        public bool IsValid { get; set; }
        public string? MD5Hash { get; set; }
        public BeatmapSetData? BeatmapSet { get; set; }
    }

    public class BeatmapSetData
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
        public DateTimeOffset LastUpdated { get; set; }
        public DateTimeOffset? RankedDate { get; set; }
        public DateTimeOffset? SubmittedDate { get; set; }
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Creator { get; set; } = "";
    }
}
