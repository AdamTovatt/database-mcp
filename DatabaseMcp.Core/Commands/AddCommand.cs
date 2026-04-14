using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Adds a new named database connection.
    /// </summary>
    public class AddCommand : ICommand
    {
        private readonly IConnectionStore _store;
        private readonly string _name;
        private readonly string _connectionString;

        /// <summary>
        /// Creates a new add command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="name">The name for the new connection.</param>
        /// <param name="connectionString">The connection string to store.</param>
        public AddCommand(IConnectionStore store, string name, string connectionString)
        {
            _store = store;
            _name = name;
            _connectionString = connectionString;
        }

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                (string normalizedConnectionString, ConnectionMetadata metadata, string providerType) =
                    ConnectionStringParser.Parse(_connectionString);

                string encrypted = _store.EncryptConnectionString(normalizedConnectionString);

                SavedConnection connection = new SavedConnection
                {
                    Name = _name,
                    ProviderType = providerType,
                    EncryptedConnectionString = encrypted,
                    Metadata = metadata,
                };

                _store.Add(connection);

                string message = $"Connection '{_name}' added ({providerType} @ {metadata.Host}:{metadata.Port}/{metadata.Database})";
                return Task.FromResult(new CommandResult(true, message));
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(new CommandResult(false, $"Invalid connection string: {ex.Message}"));
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(new CommandResult(false, ex.Message));
            }
        }
    }
}
