using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// User information (mapper, player, etc.)
    /// Based on: osu.Game/Users/RealmUser.cs
    /// </summary>
    [MapTo("RealmUser")]
    public class RealmUser : EmbeddedObject
    {
        public int OnlineID { get; set; } = 1;

        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// ISO 3166-1 alpha-2 country code
        /// Examples: "US", "JP", "GB", "XX" (unknown)
        /// </summary>
        public string CountryCode { get; set; } = "XX";

        public override string ToString() => Username;
    }
}
