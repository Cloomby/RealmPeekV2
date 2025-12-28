using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// User-specific settings for a beatmap
    /// Based on: osu.Game/Beatmaps/BeatmapUserSettings.cs
    /// </summary>
    [MapTo("BeatmapUserSettings")]
    public class BeatmapUserSettings : EmbeddedObject
    {
        /// <summary>
        /// Audio offset in milliseconds
        /// </summary>
        public double Offset { get; set; }
    }
}
