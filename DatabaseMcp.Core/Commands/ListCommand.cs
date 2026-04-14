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
        private readonly bool _includeDetails;

        /// <summary>
        /// Creates a new list command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="includeDetails">When true, includes host and database in the output. Only used by the CLI, never by MCP.</param>
        public ListCommand(IConnectionStore store, bool includeDetails = false)
        {
            _store = store;
            _includeDetails = includeDetails;
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
            string details = _includeDetails
                ? FormatDetailedTable(connections)
                : FormatConnectionTable(connections);

            return Task.FromResult(new CommandResult(true, message, details));
        }

        private static string FormatConnectionTable(List<SavedConnection> connections)
        {
            int nameWidth = Math.Max(4, connections.Max(c => c.Name.Length));
            int providerWidth = Math.Max(8, connections.Max(c => c.ProviderType.Length));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{"Name".PadRight(nameWidth)}  {"Provider".PadRight(providerWidth)}");
            sb.AppendLine($"{new string('-', nameWidth)}  {new string('-', providerWidth)}");

            foreach (SavedConnection connection in connections)
            {
                sb.AppendLine($"{connection.Name.PadRight(nameWidth)}  {connection.ProviderType.PadRight(providerWidth)}");
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatDetailedTable(List<SavedConnection> connections)
        {
            int nameWidth = Math.Max(4, connections.Max(c => c.Name.Length));
            int providerWidth = Math.Max(8, connections.Max(c => c.ProviderType.Length));
            int hostWidth = Math.Max(4, connections.Max(c => c.Metadata.Host.Length));
            int dbWidth = Math.Max(8, connections.Max(c => c.Metadata.Database.Length));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{"Name".PadRight(nameWidth)}  {"Provider".PadRight(providerWidth)}  {"Host".PadRight(hostWidth)}  {"Database".PadRight(dbWidth)}");
            sb.AppendLine($"{new string('-', nameWidth)}  {new string('-', providerWidth)}  {new string('-', hostWidth)}  {new string('-', dbWidth)}");

            foreach (SavedConnection connection in connections)
            {
                sb.AppendLine($"{connection.Name.PadRight(nameWidth)}  {connection.ProviderType.PadRight(providerWidth)}  {connection.Metadata.Host.PadRight(hostWidth)}  {connection.Metadata.Database.PadRight(dbWidth)}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
