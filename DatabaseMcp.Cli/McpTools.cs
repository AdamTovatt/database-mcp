using System.ComponentModel;
using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using ModelContextProtocol.Server;

namespace DatabaseMcp.Cli
{
    /// <summary>
    /// MCP tool definitions for the database query tool.
    /// All database operations are enforced read-only at the session level.
    /// </summary>
    [McpServerToolType]
    public class McpTools
    {
        private readonly IConnectionStore _store;
        private readonly IDatabaseProviderFactory _providerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTools"/> class.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="providerFactory">The database provider factory.</param>
        public McpTools(IConnectionStore store, IDatabaseProviderFactory providerFactory)
        {
            _store = store;
            _providerFactory = providerFactory;
        }

        /// <summary>
        /// Lists all saved database connections with safe metadata.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of connections.</returns>
        [McpServerTool(Name = "db_list_connections")]
        [Description("List all saved database connections. Shows name, provider type, host, database, and username. Never exposes passwords or connection strings. Call this first to see which connections are available.")]
        public async Task<string> ListConnectionsAsync(CancellationToken cancellationToken)
        {
            ListCommand command = new ListCommand(_store);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        /// <summary>
        /// Retrieves the full database schema for a named connection.
        /// </summary>
        /// <param name="connectionName">The name of the saved connection to inspect.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The database schema as text.</returns>
        [McpServerTool(Name = "db_schema")]
        [Description("Get the full database schema for a named connection. Returns tables, columns, data types, primary keys, and foreign key constraints. Use db_list_connections first to see available connection names.")]
        public async Task<string> GetSchemaAsync(
            [Description("The name of the saved connection (e.g. 'production', 'staging')")]
            string connectionName,
            CancellationToken cancellationToken)
        {
            SchemaCommand command = new SchemaCommand(_store, _providerFactory, connectionName);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        /// <summary>
        /// Executes a read-only SQL query against a named database connection.
        /// </summary>
        /// <param name="connectionName">The name of the saved connection to query.</param>
        /// <param name="sql">The SQL query to execute (must be read-only — SELECT only).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query results as formatted text.</returns>
        [McpServerTool(Name = "db_query")]
        [Description("Execute a read-only SQL query against a named database connection. Returns up to 1000 rows. The connection is enforced read-only at the database session level — INSERT, UPDATE, DELETE, and DDL statements are rejected by the database. Use db_schema first to understand the database structure.")]
        public async Task<string> ExecuteQueryAsync(
            [Description("The name of the saved connection (e.g. 'production', 'staging')")]
            string connectionName,
            [Description("The SQL query to execute. Must be a SELECT query — write operations are blocked at the database level.")]
            string sql,
            CancellationToken cancellationToken)
        {
            QueryCommand command = new QueryCommand(_store, _providerFactory, connectionName, sql);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        private static string FormatResult(CommandResult result)
        {
            if (string.IsNullOrEmpty(result.Details))
            {
                return result.Message;
            }

            return $"{result.Message}\n\n{result.Details}";
        }
    }
}
