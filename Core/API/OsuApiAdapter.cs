using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotenv.net;
using osu.NET;
using osu.NET.Authorization;
using osu.NET.Enums;
using osu.NET.Models.Beatmaps;

namespace RealmPeek.Core.API
{
    /// <summary>
    /// Adapter for osu.NET library by minisbett
    /// https://github.com/minisbett/osu.NET
    /// </summary>
    public class OsuApiAdapter : IDisposable
    {
        private readonly OsuApiClient _client;
        private bool _isInitialized;

        public OsuApiAdapter()
        {
            // 4. Now populate the static fields.
            // Throwing an exception is better than "??" if these are critical for the app to run.
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? throw new InvalidOperationException("OSU_CLIENT_ID is missing from environment.");
            var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? throw new InvalidOperationException("OSU_CLIENT_SECRET is missing from environment.");

            Console.WriteLine(clientId, clientSecret);

            // Create access token provider from environment variables
            OsuClientAccessTokenProvider provider = new OsuClientAccessTokenProvider(clientId, clientSecret);

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
                // Test authentication with simple request
                var result = await _client.GetUserAsync("Marble Soda");

                result.Match(
                    value =>
                    {
                        if (value != null)
                        {
                            Console.WriteLine($"✅ Authenticated with osu.NET (Found test user: {value.Username})");
                            _isInitialized = true;
                        }
                        else
                        {
                            Console.WriteLine($"❌ Authentication failed: No user returned");
                            _isInitialized = false;
                        }
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
        /// API: GET /beatmaps?ids[]=X&ids[]=Y&ids[]=Z
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
                // osu.NET supports bulk beatmap queries
                var apiResult = await _client.GetBeatmapsAsync(validIds.ToArray());

                apiResult.Match(
                    beatmaps =>
                    {
                        if (beatmaps != null)
                        {
                            foreach (var beatmap in beatmaps)
                            {
                                results.Add(ConvertToOurModel(beatmap));
                            }
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
        /// Convert osu.NET BeatmapExtended to our internal model
        /// Note: beatmap.Set is type BeatmapSet, but might be BeatmapSetExtended at runtime
        /// </summary>
        private static BeatmapData ConvertToOurModel(BeatmapExtended beatmap)
        {
            return new BeatmapData
            {
                Id = beatmap.Id,
                IsValid = true,
                MD5Hash = beatmap.Checksum,
                BeatmapSet =
                    beatmap.Set != null
                        ? new BeatmapSetData
                        {
                            // SetId is on Beatmap base class
                            Id = beatmap.SetId,
                            // Status is on BeatmapSet
                            Status = ConvertStatus(beatmap.Set.Status),
                            // Try to get dates - check if it's actually BeatmapSetExtended
                            LastUpdated = GetLastUpdated(beatmap.Set),
                            RankedDate = GetRankedDate(beatmap.Set),
                            SubmittedDate = GetSubmittedDate(beatmap.Set),
                            // Basic properties are on BeatmapSet
                            Title = beatmap.Set.Title ?? "",
                            Artist = beatmap.Set.Artist ?? "",
                            Creator = beatmap.Set.CreatorName ?? "",
                        }
                        : null,
            };
        }

        /// <summary>
        /// Get LastUpdated - only available on BeatmapSetExtended
        /// </summary>
        private static DateTimeOffset GetLastUpdated(BeatmapSet set)
        {
            // Check if it's actually BeatmapSetExtended
            if (set is BeatmapSetExtended extended)
                return extended.LastUpdated;

            // Fallback to current time if not extended
            return DateTimeOffset.Now;
        }

        /// <summary>
        /// Get RankedDate - only available on BeatmapSetExtended
        /// </summary>
        private static DateTimeOffset? GetRankedDate(BeatmapSet set)
        {
            // Check if it's actually BeatmapSetExtended
            if (set is BeatmapSetExtended extended)
                return extended.RankedDate;

            return null;
        }

        /// <summary>
        /// Get SubmittedDate - only available on BeatmapSetExtended
        /// </summary>
        private static DateTimeOffset? GetSubmittedDate(BeatmapSet set)
        {
            // Check if it's actually BeatmapSetExtended
            if (set is BeatmapSetExtended extended)
                return extended.SubmittedDate;

            return null;
        }

        /// <summary>
        /// Convert osu.NET RankedStatus enum to string
        /// </summary>
        private static string ConvertStatus(RankedStatus status)
        {
            return status switch
            {
                RankedStatus.Ranked => "ranked",
                RankedStatus.Approved => "approved",
                RankedStatus.Qualified => "qualified",
                RankedStatus.Loved => "loved",
                RankedStatus.Graveyard => "graveyard",
                RankedStatus.WIP => "wip",
                RankedStatus.Pending => "pending",
                _ => "unknown",
            };
        }

        public void Dispose()
        {
            // OsuApiClient doesn't implement IDisposable
            // No-op for interface compatibility
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
