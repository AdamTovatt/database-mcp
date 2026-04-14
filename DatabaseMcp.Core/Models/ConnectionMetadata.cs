namespace DatabaseMcp.Core.Models
{
    /// <summary>
    /// Display-safe connection metadata extracted from the connection string.
    /// These fields are stored in plaintext alongside the encrypted connection string
    /// so that connection info can be displayed without decryption.
    /// </summary>
    /// <param name="Host">The database server hostname.</param>
    /// <param name="Port">The database server port.</param>
    /// <param name="Database">The database name.</param>
    /// <param name="Username">The database username.</param>
    public record ConnectionMetadata(string Host, int Port, string Database, string Username);
}
