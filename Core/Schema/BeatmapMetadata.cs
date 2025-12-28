using Realms;
using System.Collections.Generic;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Metadata for a beatmap (title, artist, etc.)
    /// NOTE: This is a RealmObject (TopLevel), NOT EmbeddedObject!
    /// </summary>
    public class BeatmapMetadata : RealmObject
    {
        public string? Title { get; set; }

        public string? TitleUnicode { get; set; }

        public string? Artist { get; set; }

        public string? ArtistUnicode { get; set; }

        public RealmUser? Author { get; set; }

        public string? Source { get; set; }

        public string? Tags { get; set; }

        public int PreviewTime { get; set; }

        public string? AudioFile { get; set; }

        public string? BackgroundFile { get; set; }

        // Required for schema v51
        public IList<string>? UserTags { get; }
    }
}
