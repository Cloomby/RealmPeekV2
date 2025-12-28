using RealmPeek.Core.Schema;

namespace RealmPeek.Core.Models
{
    /// <summary>
    /// Domain model representing a beatmap set
    /// Clean POCO without Realm dependencies
    /// </summary>
    public class BeatmapSetModel
    {
        public Guid ID { get; set; }
        public int OnlineID { get; set; }
        public Status Status { get; set; }
        public string Title { get; set; } = "[Unknown]";
        public string Artist { get; set; } = "[Unknown]";
        public string Creator { get; set; } = "[Unknown]";
        public DateTimeOffset? DateRanked { get; set; }
        public DateTimeOffset? DateSubmitted { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public List<BeatmapModel> Maps { get; set; } = new();

        // Computed properties for business logic
        public bool HasValidOnlineID => OnlineID > 0;
        public bool HasValidMapID => Maps.Any(m => m.OnlineID > 0);
        public int FirstValidMapID => Maps.FirstOrDefault(m => m.OnlineID > 0)?.OnlineID ?? 0;
        public bool HasZeroStarRatings => Maps.Any(m => m.StarRating == 0);
        public bool NeedsDates => !DateSubmitted.HasValue || !DateRanked.HasValue;
        public bool IsRankedFamily => Status == Status.Ranked || Status == Status.Approved || Status == Status.Loved;

        public override string ToString() => $"{Artist} - {Title} (by {Creator}) [ID: {OnlineID}]";
    }

    /// <summary>
    /// Domain model representing an individual beatmap (difficulty)
    /// </summary>
    public class BeatmapModel
    {
        public Guid ID { get; set; }
        public string? DifficultyName { get; set; }
        public Ruleset? Ruleset { get; set; }
        public BeatmapDifficulty? Difficulty { get; set; }
        public BeatmapMetadata? Metadata { get; set; }
        public BeatmapUserSettings? UserSettings { get; set; }
        public BeatmapSet? BeatmapSet { get; set; } // Inverse relationship
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

        public override string ToString() => $"{DifficultyName} [{StarRating:F2}★]";
    }
}
