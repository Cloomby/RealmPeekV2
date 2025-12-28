using Realms;

namespace RealmPeek.Core.Schema
{
    /// <summary>
    /// Links a logical filename to a physical file hash
    /// </summary>
    public class RealmNamedFileUsage : EmbeddedObject
    {
        public File? File { get; set; }

        public string? Filename { get; set; }
    }
}
