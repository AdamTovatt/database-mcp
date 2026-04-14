using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Interfaces
{
    /// <summary>
    /// Manages persistent storage of named database connections.
    /// Connection strings are stored encrypted to prevent accidental exposure.
    /// </summary>
    public interface IConnectionStore
    {
        /// <summary>
        /// Returns all saved connections.
        /// </summary>
        List<SavedConnection> GetAll();

        /// <summary>
        /// Returns the connection with the specified name, or null if not found.
        /// </summary>
        /// <param name="name">The connection name.</param>
        SavedConnection? GetByName(string name);

        /// <summary>
        /// Adds a new named connection. Throws if a connection with the same name already exists.
        /// </summary>
        /// <param name="connection">The connection to add.</param>
        /// <exception cref="InvalidOperationException">Thrown if the name already exists.</exception>
        void Add(SavedConnection connection);

        /// <summary>
        /// Removes the connection with the specified name.
        /// </summary>
        /// <param name="name">The connection name.</param>
        /// <returns>True if the connection was found and removed, false if not found.</returns>
        bool Remove(string name);

        /// <summary>
        /// Encrypts a connection string for storage.
        /// </summary>
        /// <param name="connectionString">The plaintext connection string.</param>
        /// <returns>The encrypted connection string (Base64-encoded).</returns>
        string EncryptConnectionString(string connectionString);

        /// <summary>
        /// Decrypts a stored connection string.
        /// </summary>
        /// <param name="encryptedConnectionString">The encrypted connection string (Base64-encoded).</param>
        /// <returns>The plaintext connection string.</returns>
        string DecryptConnectionString(string encryptedConnectionString);
    }
}
