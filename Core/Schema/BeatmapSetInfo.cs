using System;
using System.Collections.Generic;
using System.Linq;
using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Represents a beatmap set (collection of difficulties)
    /// Based on: osu.Game/Beatmaps/BeatmapSetInfo.cs
    /// </summary>
    [MapTo("BeatmapSet")]
    public class BeatmapSetInfo : RealmObject
    {
        [PrimaryKey]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Indexed]
        public int OnlineID { get; set; } = -1;

        public DateTimeOffset DateAdded { get; set; }

        public DateTimeOffset? DateSubmitted { get; set; }

        public DateTimeOffset? DateRanked { get; set; }

        public IList<BeatmapInfo> Beatmaps { get; } = null!;

        public IList<RealmNamedFileUsage> Files { get; } = null!;

        public int Status { get; set; } = (int)BeatmapOnlineStatus.None;

        public bool DeletePending { get; set; }

        public string Hash { get; set; } = string.Empty;

        public bool Protected { get; set; }

        // Computed properties
        [Ignored]
        public BeatmapOnlineStatus OnlineStatus
        {
            get => (BeatmapOnlineStatus)Status;
            set => Status = (int)value;
        }

        [Ignored]
        public string Title => Beatmaps.FirstOrDefault()?.Metadata?.Title ?? "[Unknown]";

        [Ignored]
        public string Artist => Beatmaps.FirstOrDefault()?.Metadata?.Artist ?? "[Unknown]";

        [Ignored]
        public string Creator => Beatmaps.FirstOrDefault()?.Metadata?.Author?.Username ?? "[Unknown]";

        public override string ToString() => $"{Artist} - {Title} (by {Creator})";
    }

    /// <summary>
    /// Beatmap online status enum
    /// Matches osu.Game/Beatmaps/BeatmapOnlineStatus.cs
    /// </summary>
    public enum BeatmapOnlineStatus
    {
        None = -3,
        Graveyard = -2,
        WIP = -1,
        Pending = 0,
        Ranked = 1,
        Approved = 2,
        Qualified = 3,
        Loved = 4,
        LocallyModified = -4,
    }
}
