namespace DatabaseMcp.Core.Models
{
    /// <summary>
    /// Represents a named database connection stored in the config file.
    /// The connection string is encrypted; only metadata is stored in plaintext.
    /// </summary>
    public class SavedConnection
    {
        /// <summary>
        /// The user-assigned name for this connection (e.g. "production", "staging").
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The database provider type (e.g. "postgres", "sqlite").
        /// </summary>
        public required string ProviderType { get; set; }

        /// <summary>
        /// The AES-256 encrypted connection string (Base64-encoded).
        /// </summary>
        public required string EncryptedConnectionString { get; set; }

        /// <summary>
        /// Display-safe metadata extracted from the connection string at add time.
        /// </summary>
        public required ConnectionMetadata Metadata { get; set; }
    }
}
