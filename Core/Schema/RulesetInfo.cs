using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Game mode (osu!, taiko, catch, mania)
    /// Based on: osu.Game/Rulesets/RulesetInfo.cs
    /// </summary>
    [MapTo("Ruleset")]
    public class RulesetInfo : RealmObject
    {
        /// <summary>
        /// Short name used as primary key
        /// Examples: "osu", "taiko", "fruits", "mania"
        /// </summary>
        [PrimaryKey]
        public string ShortName { get; set; } = string.Empty;

        [Indexed]
        public int OnlineID { get; set; } = -1;

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// .NET type information for instantiating the ruleset
        /// </summary>
        public string InstantiationInfo { get; set; } = string.Empty;

        public int LastAppliedDifficultyVersion { get; set; }

        public bool Available { get; set; }

        public override string ToString() => Name;
    }
}
