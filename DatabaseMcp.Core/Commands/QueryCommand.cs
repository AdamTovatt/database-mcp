using System.Text;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Executes a read-only SQL query against a saved database connection.
    /// </summary>
    public class QueryCommand : ICommand
    {
        private readonly IConnectionStore _store;
        private readonly IDatabaseProviderFactory _providerFactory;
        private readonly string _name;
        private readonly string _sql;
        private readonly int _maxRows;

        /// <summary>
        /// Creates a new query command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="providerFactory">The database provider factory.</param>
        /// <param name="name">The name of the connection to query.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="maxRows">Maximum number of rows to return (default 1000).</param>
        public QueryCommand(IConnectionStore store, IDatabaseProviderFactory providerFactory, string name, string sql, int maxRows = 1000)
        {
            _store = store;
            _providerFactory = providerFactory;
            _name = name;
            _sql = sql;
            _maxRows = maxRows;
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
                QueryResult result = await provider.ExecuteQueryAsync(connectionString, _sql, _maxRows, cancellationToken);

                if (result.Columns.Count == 0)
                {
                    return new CommandResult(true, "Query executed successfully. No columns returned.");
                }

                string truncatedNote = result.Truncated ? $" (truncated to {_maxRows} rows)" : "";
                string message = $"{result.RowCount} row(s) returned{truncatedNote}";
                string details = FormatQueryResult(result);

                return new CommandResult(true, message, details);
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"Query failed: {ex.Message}");
            }
        }

        private static string FormatQueryResult(QueryResult result)
        {
            if (result.Rows.Count == 0)
            {
                return string.Join("  ", result.Columns);
            }

            Dictionary<string, int> columnWidths = new Dictionary<string, int>();
            foreach (string column in result.Columns)
            {
                int maxValueWidth = result.Rows
                    .Select(row => FormatValue(row.GetValueOrDefault(column)).Length)
                    .DefaultIfEmpty(0)
                    .Max();
                columnWidths[column] = Math.Max(column.Length, maxValueWidth);
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Join("  ", result.Columns.Select(c => c.PadRight(columnWidths[c]))));
            sb.AppendLine(string.Join("  ", result.Columns.Select(c => new string('-', columnWidths[c]))));

            foreach (Dictionary<string, object?> row in result.Rows)
            {
                sb.AppendLine(string.Join("  ", result.Columns.Select(c =>
                    FormatValue(row.GetValueOrDefault(c)).PadRight(columnWidths[c]))));
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
            {
                return "NULL";
            }

            if (value is DateTime dt)
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (value is DateTimeOffset dto)
            {
                return dto.ToString("yyyy-MM-dd HH:mm:ss zzz");
            }

            if (value is bool b)
            {
                return b ? "true" : "false";
            }

            return value.ToString() ?? "NULL";
        }
    }
}
