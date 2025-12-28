//using Realms;

//namespace RealmPeek.Schema
//{
//    public class BeatmapMetadata : RealmObject
//    {
//        public string? Title { get; set; }
//        public string? TitleUnicode { get; set; }
//        public string? Artist { get; set; }
//        public string? ArtistUnicode { get; set; }
//        public RealmUser? Author { get; set; }
//        public string? Source { get; set; }
//        public string? Tags { get; set; }
//        public int PreviewTime { get; set; }
//        public string? AudioFile { get; set; }
//        public string? BackgroundFile { get; set; }
//        public IList<string>? UserTags { get; }
//    }

//    public class BeatmapSet : RealmObject
//    {
//        [PrimaryKey]
//        public Guid ID { get; set; }
//        public int OnlineID { get; set; }
//        public DateTimeOffset DateAdded { get; set; }
//        public DateTimeOffset? DateSubmitted { get; set; }
//        public DateTimeOffset? DateRanked { get; set; }
//        public IList<Beatmap> Beatmaps { get; }
//        public IList<RealmNamedFileUsage> Files { get; }
//        public int Status { get; set; }
//        public bool DeletePending { get; set; }
//        public string? Hash { get; set; }
//        public bool Protected { get; set; }
//    }

//    public class Beatmap : RealmObject
//    {
//        [PrimaryKey]
//        public Guid ID { get; set; }
//        public string? DifficultyName { get; set; }
//        public Ruleset? Ruleset { get; set; }
//        public BeatmapDifficulty? Difficulty { get; set; }
//        public BeatmapMetadata Metadata { get; set; }
//        public BeatmapUserSettings? UserSettings { get; set; }
//        public BeatmapSet? BeatmapSet { get; set; } // Inverse
//        public int Status { get; set; }
//        public int OnlineID { get; set; }
//        public double Length { get; set; }
//        public double BPM { get; set; }
//        public string? Hash { get; set; }
//        public double StarRating { get; set; }
//        public string? MD5Hash { get; set; }
//        public string? OnlineMD5Hash { get; set; }
//        public DateTimeOffset? LastLocalUpdate { get; set; }
//        public DateTimeOffset? LastOnlineUpdate { get; set; }
//        public bool Hidden { get; set; }
//        public int EndTimeObjectCount { get; set; }
//        public int TotalObjectCount { get; set; }
//        public DateTimeOffset? LastPlayed { get; set; }
//        public int BeatDivisor { get; set; }
//        public double? EditorTimestamp { get; set; }
//    }

//    public class RealmUser : EmbeddedObject
//    {
//        public int OnlineID { get; set; }
//        public string? Username { get; set; }
//        public string? CountryCode { get; set; }
//    }

//    public class Ruleset : RealmObject
//    {
//        [PrimaryKey]
//        public string? ShortName { get; set; }
//        public int OnlineID { get; set; }
//        public string? Name { get; set; }
//        public string? InstantiationInfo { get; set; }
//        public int LastAppliedDifficultyVersion { get; set; }
//        public bool Available { get; set; }
//    }

//    public class BeatmapDifficulty : EmbeddedObject
//    {
//        public float DrainRate { get; set; }
//        public float CircleSize { get; set; }
//        public float OverallDifficulty { get; set; }
//        public float ApproachRate { get; set; }
//        public double SliderMultiplier { get; set; }
//        public double SliderTickRate { get; set; }
//    }

//    public class BeatmapUserSettings : EmbeddedObject
//    {
//        public double Offset { get; set; }
//    }

//    public class RealmNamedFileUsage : EmbeddedObject
//    {
//        public File? File { get; set; }
//        public string? Filename { get; set; }
//    }

//    public class File : RealmObject
//    {
//        [PrimaryKey]
//        public string? Hash { get; set; }
//    }
//}
