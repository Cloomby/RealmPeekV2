using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Content-addressable file storage
    /// Files are stored in files/ directory named by their SHA-256 hash
    /// Based on: osu.Game/IO/RealmFile.cs
    /// </summary>
    [MapTo("File")]
    public class RealmFile : RealmObject
    {
        /// <summary>
        /// SHA-256 hash of file content (primary key)
        /// Example: "a1b2c3d4e5f6..." (64 hex characters)
        /// Physical location: files/a/a1/a1b2c3d4e5f6...
        /// </summary>
        [PrimaryKey]
        public string Hash { get; set; } = string.Empty;

        public override string ToString() => $"File({Hash[..8]}...)";
    }
}
