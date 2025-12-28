using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Content-addressable file storage
    /// Files are stored by their SHA-256 hash
    /// </summary>
    public class File : RealmObject
    {
        [PrimaryKey]
        public string? Hash { get; set; }
    }
}
