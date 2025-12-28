using System;
using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Represents an individual beatmap (difficulty)
    /// NOTE: No [MapTo] attribute - class name must be "Beatmap"
    /// </summary>
    public class Beatmap : RealmObject
    {
        [PrimaryKey]
        public Guid ID { get; set; }

        public string? DifficultyName { get; set; }

        public Ruleset? Ruleset { get; set; }

        public BeatmapDifficulty? Difficulty { get; set; }

        public BeatmapMetadata? Metadata { get; set; }

        public BeatmapUserSettings? UserSettings { get; set; }

        // Inverse relationship - MUST exist for schema v51
        public BeatmapSet? BeatmapSet { get; set; }

        public int Status { get; set; }

        public int OnlineID { get; set; }

        public double Length { get; set; }

        public double BPM { get; set; }

        public string? Hash { get; set; }

        public double StarRating { get; set; }

        public string? MD5Hash { get; set; }

        public string? OnlineMD5Hash { get; set; }

        public DateTimeOffset? LastLocalUpdate { get; set; }

        public DateTimeOffset? LastOnlineUpdate { get; set; }

        public bool Hidden { get; set; }

        public int EndTimeObjectCount { get; set; }

        public int TotalObjectCount { get; set; }

        public DateTimeOffset? LastPlayed { get; set; }

        public int BeatDivisor { get; set; }

        public double? EditorTimestamp { get; set; }
    }
}
