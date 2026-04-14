using System.Text;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Lists all saved database connections with display-safe metadata.
    /// </summary>
    public class ListCommand : ICommand
    {
        private readonly IConnectionStore _store;

        /// <summary>
        /// Creates a new list command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        public ListCommand(IConnectionStore store)
        {
            _store = store;
        }

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            List<SavedConnection> connections = _store.GetAll();

            if (connections.Count == 0)
            {
                return Task.FromResult(new CommandResult(true,
                    "No connections saved. Use 'db add <name> <connection-string>' to add one."));
            }

            string message = $"{connections.Count} saved connection(s):";
            string details = FormatConnectionTable(connections);

            return Task.FromResult(new CommandResult(true, message, details));
        }

        private static string FormatConnectionTable(List<SavedConnection> connections)
        {
            int nameWidth = Math.Max(4, connections.Max(c => c.Name.Length));
            int providerWidth = Math.Max(8, connections.Max(c => c.ProviderType.Length));
            int hostWidth = Math.Max(4, connections.Max(c => c.Metadata.Host.Length));
            int dbWidth = Math.Max(8, connections.Max(c => c.Metadata.Database.Length));
            int userWidth = Math.Max(8, connections.Max(c => c.Metadata.Username.Length));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{"Name".PadRight(nameWidth)}  {"Provider".PadRight(providerWidth)}  {"Host".PadRight(hostWidth)}  {"Database".PadRight(dbWidth)}  {"Username".PadRight(userWidth)}");
            sb.AppendLine($"{new string('-', nameWidth)}  {new string('-', providerWidth)}  {new string('-', hostWidth)}  {new string('-', dbWidth)}  {new string('-', userWidth)}");

            foreach (SavedConnection connection in connections)
            {
                sb.AppendLine($"{connection.Name.PadRight(nameWidth)}  {connection.ProviderType.PadRight(providerWidth)}  {connection.Metadata.Host.PadRight(hostWidth)}  {connection.Metadata.Database.PadRight(dbWidth)}  {connection.Metadata.Username.PadRight(userWidth)}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
