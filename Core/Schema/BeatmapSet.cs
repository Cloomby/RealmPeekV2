using System;
using System.Collections.Generic;
using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Represents a beatmap set (collection of difficulties)
    /// NOTE: No [MapTo] attribute - class name must be "BeatmapSet"
    /// </summary>
    public class BeatmapSet : RealmObject
    {
        [PrimaryKey]
        public Guid ID { get; set; }

        public int OnlineID { get; set; }

        public DateTimeOffset DateAdded { get; set; }

        public DateTimeOffset? DateSubmitted { get; set; }

        public DateTimeOffset? DateRanked { get; set; }

        public IList<Beatmap> Beatmaps { get; }

        // Must be RealmNamedFileUsage, not BeatmapSetFileInfo
        public IList<RealmNamedFileUsage> Files { get; }

        public int Status { get; set; }

        public bool DeletePending { get; set; }

        public string? Hash { get; set; }

        public bool Protected { get; set; }
    }
}
