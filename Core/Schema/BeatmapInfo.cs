using System;
using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Represents an individual beatmap (difficulty)
    /// Based on: osu.Game/Beatmaps/BeatmapInfo.cs
    /// </summary>
    [MapTo("Beatmap")]
    public class BeatmapInfo : RealmObject
    {
        [PrimaryKey]
        public Guid ID { get; set; } = Guid.NewGuid();

        public string DifficultyName { get; set; } = string.Empty;

        public RulesetInfo Ruleset { get; set; } = null!;

        public BeatmapDifficulty Difficulty { get; set; } = null!;

        public BeatmapMetadata Metadata { get; set; } = null!;

        public BeatmapUserSettings UserSettings { get; set; } = null!;

        /// <summary>
        /// Parent beatmap set (inverse relationship)
        /// Realm manages this automatically via [Backlink]
        /// </summary>
        [Backlink(nameof(BeatmapSetInfo.Beatmaps))]
        public BeatmapSetInfo? BeatmapSet { get; set; }

        public int Status { get; set; } = (int)BeatmapOnlineStatus.None;

        [Indexed]
        public int OnlineID { get; set; } = -1;

        public double Length { get; set; }

        public double BPM { get; set; }

        public string Hash { get; set; } = string.Empty;

        public double StarRating { get; set; } = -1;

        public string MD5Hash { get; set; } = string.Empty;

        public string OnlineMD5Hash { get; set; } = string.Empty;

        public DateTimeOffset? LastLocalUpdate { get; set; }

        public DateTimeOffset? LastOnlineUpdate { get; set; }

        public bool Hidden { get; set; }

        public int EndTimeObjectCount { get; set; }

        public int TotalObjectCount { get; set; }

        public DateTimeOffset? LastPlayed { get; set; }

        public int BeatDivisor { get; set; }

        public double? EditorTimestamp { get; set; }

        // Computed properties
        [Ignored]
        public BeatmapOnlineStatus OnlineStatus
        {
            get => (BeatmapOnlineStatus)Status;
            set => Status = (int)value;
        }

        public override string ToString() => $"{DifficultyName} [{StarRating:F2}★]";
    }
}
