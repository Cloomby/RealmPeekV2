using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Difficulty settings for a beatmap (CS, AR, OD, HP)
    /// Based on: osu.Game/Beatmaps/BeatmapDifficulty.cs
    /// </summary>
    [MapTo("BeatmapDifficulty")]
    public class BeatmapDifficulty : EmbeddedObject
    {
        /// <summary>
        /// HP drain rate (health)
        /// </summary>
        public float DrainRate { get; set; } = 5;

        /// <summary>
        /// Circle size
        /// </summary>
        public float CircleSize { get; set; } = 5;

        /// <summary>
        /// Overall difficulty (timing windows)
        /// </summary>
        public float OverallDifficulty { get; set; } = 5;

        /// <summary>
        /// Approach rate (how fast circles appear)
        /// </summary>
        public float ApproachRate { get; set; } = 5;

        public double SliderMultiplier { get; set; } = 1.4;

        public double SliderTickRate { get; set; } = 1;

        public override string ToString() => $"CS:{CircleSize} AR:{ApproachRate} OD:{OverallDifficulty} HP:{DrainRate}";
    }
}
