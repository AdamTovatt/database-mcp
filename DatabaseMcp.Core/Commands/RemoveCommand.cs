using DatabaseMcp.Core.Interfaces;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Removes a saved database connection by name.
    /// </summary>
    public class RemoveCommand : ICommand
    {
        private readonly IConnectionStore _store;
        private readonly string _name;

        /// <summary>
        /// Creates a new remove command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="name">The name of the connection to remove.</param>
        public RemoveCommand(IConnectionStore store, string name)
        {
            _store = store;
            _name = name;
        }

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            bool removed = _store.Remove(_name);

            if (!removed)
            {
                return Task.FromResult(new CommandResult(false, $"Connection '{_name}' not found."));
            }

            return Task.FromResult(new CommandResult(true, $"Connection '{_name}' removed."));
        }
    }
}
