using System.Text;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Retrieves and displays the database schema for a saved connection.
    /// </summary>
    public class SchemaCommand : ICommand
    {
        private readonly IConnectionStore _store;
        private readonly IDatabaseProviderFactory _providerFactory;
        private readonly string _name;

        /// <summary>
        /// Creates a new schema command.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="providerFactory">The database provider factory.</param>
        /// <param name="name">The name of the connection to inspect.</param>
        public SchemaCommand(IConnectionStore store, IDatabaseProviderFactory providerFactory, string name)
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
                SchemaInfo schema = await provider.GetSchemaAsync(connectionString, cancellationToken);

                string message = $"Schema for '{_name}': {schema.Tables.Count} table(s)";
                string details = FormatSchema(schema);

                return new CommandResult(true, message, details);
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"Failed to retrieve schema for '{_name}': {ex.Message}");
            }
        }

        private static string FormatSchema(SchemaInfo schema)
        {
            StringBuilder sb = new StringBuilder();

            foreach (TableInfo table in schema.Tables)
            {
                sb.AppendLine($"Table: {table.SchemaName}.{table.TableName}");

                foreach (ColumnInfo column in table.Columns)
                {
                    string nullable = column.IsNullable ? "NULL" : "NOT NULL";
                    string pk = column.IsPrimaryKey ? " [PK]" : "";
                    string defaultVal = column.ColumnDefault != null ? $" DEFAULT {column.ColumnDefault}" : "";
                    sb.AppendLine($"  {column.Name} {column.DataType} {nullable}{pk}{defaultVal}");
                }

                if (table.ForeignKeys.Count > 0)
                {
                    sb.AppendLine("  Foreign keys:");
                    foreach (ForeignKeyInfo fk in table.ForeignKeys)
                    {
                        sb.AppendLine($"    {fk.ColumnName} -> {fk.ReferencedTable}.{fk.ReferencedColumn} ({fk.ConstraintName})");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}
