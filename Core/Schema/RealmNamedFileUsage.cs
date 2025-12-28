using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Links a logical filename to a physical file hash
    /// Based on: osu.Game/Database/RealmNamedFileUsage.cs
    /// </summary>
    [MapTo("BeatmapSetFileInfo")]
    public class RealmNamedFileUsage : EmbeddedObject
    {
        /// <summary>
        /// Reference to the actual file (by hash)
        /// </summary>
        public RealmFile File { get; set; } = null!;

        /// <summary>
        /// Logical filename (e.g., "audio.mp3", "bg.jpg", "Hard.osu")
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        public override string ToString() => $"{Filename} ({File?.Hash?[..8]}...)";
    }
}
