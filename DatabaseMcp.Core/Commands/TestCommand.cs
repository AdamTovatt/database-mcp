using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Tests that a saved database connection can be established.
    /// </summary>
    public class TestCommand : ICommand
    {
        private readonly IConnectionStore _store;
        private readonly IDatabaseProviderFactory _providerFactory;
        private readonly string _name;

        /// <summary>
        /// Creates a new test command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="providerFactory">The database provider factory.</param>
        /// <param name="name">The name of the connection to test.</param>
        public TestCommand(IConnectionStore store, IDatabaseProviderFactory providerFactory, string name)
        {
            _store = store;
            _providerFactory = providerFactory;
            _name = name;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            SavedConnection? connection = _store.GetByName(_name);
            if (connection == null)
            {
                return new CommandResult(false, $"Connection '{_name}' not found. Use 'db list' to see available connections.");
            }

            try
            {
                string connectionString = _store.DecryptConnectionString(connection.EncryptedConnectionString);
                IDatabaseProvider provider = _providerFactory.Create(connection.ProviderType);
                await provider.TestConnectionAsync(connectionString, cancellationToken);
                return new CommandResult(true, $"Connection '{_name}' is working ({connection.ProviderType} @ {connection.Metadata.Host}:{connection.Metadata.Port}/{connection.Metadata.Database}).");
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"Connection '{_name}' failed: {ex.Message}");
            }
        }
    }
}
