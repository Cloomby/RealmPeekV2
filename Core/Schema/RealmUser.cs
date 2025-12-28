using Realms;

namespace RealmPeek.Core.Schema
{
    public class RealmUser : EmbeddedObject
    {
        public int OnlineID { get; set; }

        public string? Username { get; set; }

        public string? CountryCode { get; set; }
    }
}
