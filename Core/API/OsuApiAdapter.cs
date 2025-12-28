using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.NET;
using osu.NET.Authorization;
using osu.NET.Enums;
using osu.NET.Models;

namespace RealmPeek.Core.API
{
    /// <summary>
    /// Adapter for osu.NET library by minisbett
    /// https://github.com/minisbett/osu.NET
    ///
    /// Install via: dotnet add package osu.NET
    /// </summary>
    public class OsuApiAdapter : IDisposable
    {
        private readonly OsuApiClient _client;
        private bool _isInitialized;

        public OsuApiAdapter()
        {
            // Create access token provider using client credentials
            OsuClientAccessTokenProvider provider = OsuClientAccessTokenProvider.FromEnvironmentVariables("OSU_CLIENT_ID", "OSU_CLIENT_SECRET");

            // Create client (null logger for standalone use)
            _client = new OsuApiClient(provider, null);
        }

        /// <summary>
        /// Authenticate with osu! API
        /// osu.NET handles token management automatically
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                // Test authentication with a simple request
                // peppy's user ID
                var result = await _client.GetUserAsync("peppy");

                result.Match(
                    value =>
                    {
                        Console.WriteLine($"✅ Authenticated with osu.NET (tested with user: {value.Username})");
                        _isInitialized = true;
                    },
                    error =>
                    {
                        Console.WriteLine($"❌ Authentication failed: {error.Message}");
                        _isInitialized = false;
                    }
                );

                return _isInitialized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Authentication error: {ex.Message}");
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Get beatmaps by IDs using osu.NET
        /// NOTE: osu.NET DOES support bulk queries via GetBeatmapsAsync(ids[])
        /// </summary>
        public async Task<List<BeatmapData>> GetBeatmapsAsync(IEnumerable<int> ids)
        {
            if (!_isInitialized)
            {
                Console.WriteLine("Not authenticated, attempting to authenticate...");
                if (!await AuthenticateAsync())
                    return new List<BeatmapData>();
            }

            var results = new List<BeatmapData>();
            var validIds = ids.Where(x => x > 0).Distinct().ToList();

            if (!validIds.Any())
                return results;

            try
            {
                // osu.NET supports bulk beatmap queries!
                // API: GET /beatmaps?ids[]=X&ids[]=Y&ids[]=Z
                var apiResult = await _client.GetBeatmapsAsync(validIds.ToArray());

                apiResult.Match(
                    beatmaps =>
                    {
                        // Convert osu.NET models to our internal models
                        foreach (var beatmap in beatmaps)
                        {
                            results.Add(ConvertToOurModel(beatmap));
                        }
                    },
                    error =>
                    {
                        Console.WriteLine($"API Error: {error.Message}");
                    }
                );

                // Add dummy entries for IDs not found
                var foundIds = new HashSet<int>(results.Select(r => r.Id));
                foreach (var id in validIds.Where(id => !foundIds.Contains(id)))
                {
                    results.Add(new BeatmapData { Id = id, IsValid = false });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching beatmaps: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Convert osu.NET Beatmap to our internal model
        /// </summary>
        private BeatmapData ConvertToOurModel(Beatmap beatmap)
        {
            return new BeatmapData
            {
                Id = beatmap.Id,
                IsValid = true,
                MD5Hash = beatmap.Checksum,
                BeatmapSet =
                    beatmap.Beatmapset != null
                        ? new BeatmapSetData
                        {
                            Id = beatmap.BeatmapsetId,
                            Status = ConvertStatus(beatmap.Status),
                            LastUpdated = beatmap.Beatmapset.LastUpdated,
                            RankedDate = beatmap.Beatmapset.RankedDate,
                            SubmittedDate = beatmap.Beatmapset.SubmittedDate,
                            Title = beatmap.Beatmapset.Title ?? "",
                            Artist = beatmap.Beatmapset.Artist ?? "",
                            Creator = beatmap.Beatmapset.Creator ?? "",
                        }
                        : null,
            };
        }

        /// <summary>
        /// Convert osu.NET BeatmapStatus to string for our Status enum
        /// </summary>
        private string ConvertStatus(string status)
        {
            // osu.NET returns status as string: "ranked", "loved", etc.
            return status.ToLowerInvariant();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    /// <summary>
    /// Our internal beatmap data model
    /// Decoupled from osu.NET library
    /// </summary>
    public class BeatmapData
    {
        public int Id { get; set; }
        public bool IsValid { get; set; }
        public string? MD5Hash { get; set; }
        public BeatmapSetData? BeatmapSet { get; set; }
    }

    /// <summary>
    /// Our internal beatmap set data model
    /// </summary>
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
