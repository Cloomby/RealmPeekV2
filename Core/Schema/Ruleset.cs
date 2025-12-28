using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Game mode (osu!, taiko, catch, mania)
    /// </summary>
    public class Ruleset : RealmObject
    {
        [PrimaryKey]
        public string? ShortName { get; set; }

        public int OnlineID { get; set; }

        public string? Name { get; set; }

        public string? InstantiationInfo { get; set; }

        public int LastAppliedDifficultyVersion { get; set; }

        public bool Available { get; set; }
    }
}
