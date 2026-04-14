using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Interfaces
{
    /// <summary>
    /// Abstraction for database-specific operations.
    /// Each supported database engine implements this interface.
    /// All connections opened by providers are enforced read-only at the database level.
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Tests that the connection can be established successfully.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task TestConnectionAsync(string connectionString, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the full database schema including tables, columns, types, and foreign keys.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The database schema information.</returns>
        Task<SchemaInfo> GetSchemaAsync(string connectionString, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a read-only SQL query and returns the results.
        /// The connection is enforced read-only at the database session level.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="maxRows">Maximum number of rows to return.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The query results.</returns>
        Task<QueryResult> ExecuteQueryAsync(string connectionString, string sql, int maxRows, CancellationToken cancellationToken);
    }
}
