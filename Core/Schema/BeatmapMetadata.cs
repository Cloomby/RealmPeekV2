using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Metadata for a beatmap (title, artist, etc.)
    /// Based on: osu.Game/Beatmaps/BeatmapMetadata.cs
    /// Note: This is an embedded object, NOT a separate table
    /// Each BeatmapInfo has its own BeatmapMetadata instance
    /// </summary>
    [MapTo("BeatmapMetadata")]
    public class BeatmapMetadata : EmbeddedObject
    {
        public string Title { get; set; } = string.Empty;

        public string TitleUnicode { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        public string ArtistUnicode { get; set; } = string.Empty;

        public RealmUser Author { get; set; } = new RealmUser();

        public string Source { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty;

        public int PreviewTime { get; set; } = -1;

        public string AudioFile { get; set; } = string.Empty;

        public string BackgroundFile { get; set; } = string.Empty;

        public override string ToString() => $"{Artist} - {Title}";
    }
}
