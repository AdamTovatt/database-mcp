using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using Npgsql;

namespace DatabaseMcp.Core.Providers
{
    /// <summary>
    /// PostgreSQL database provider using Npgsql.
    /// All connections are enforced read-only at the database session level
    /// via <c>SET default_transaction_read_only = on</c>.
    /// </summary>
    public class PostgresProvider : IDatabaseProvider
    {
        /// <inheritdoc/>
        public async Task TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnforceReadOnly(connection, cancellationToken);

            await using NpgsqlCommand command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<SchemaInfo> GetSchemaAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnforceReadOnly(connection, cancellationToken);

            SchemaInfo schema = new SchemaInfo();

            List<TableInfo> tables = await GetTablesAsync(connection, cancellationToken);
            Dictionary<string, List<string>> primaryKeys = await GetPrimaryKeysAsync(connection, cancellationToken);
            Dictionary<string, List<ForeignKeyInfo>> foreignKeys = await GetForeignKeysAsync(connection, cancellationToken);

            foreach (TableInfo table in tables)
            {
                string tableKey = $"{table.SchemaName}.{table.TableName}";

                if (primaryKeys.TryGetValue(tableKey, out List<string>? pkColumns))
                {
                    foreach (ColumnInfo column in table.Columns)
                    {
                        column.IsPrimaryKey = pkColumns.Contains(column.Name);
                    }
                }

                if (foreignKeys.TryGetValue(tableKey, out List<ForeignKeyInfo>? fks))
                {
                    table.ForeignKeys = fks;
                }

                schema.Tables.Add(table);
            }

            return schema;
        }

        /// <inheritdoc/>
        public async Task<QueryResult> ExecuteQueryAsync(string connectionString, string sql, int maxRows, CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnforceReadOnly(connection, cancellationToken);
            await SetStatementTimeout(connection, cancellationToken);

            await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            QueryResult result = new QueryResult();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            int rowsRead = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                rowsRead++;
                if (rowsRead > maxRows)
                {
                    result.Truncated = true;
                    break;
                }

                Dictionary<string, object?> row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    object value = reader.GetValue(i);
                    row[result.Columns[i]] = value == DBNull.Value ? null : value;
                }

                result.Rows.Add(row);
            }

            result.RowCount = result.Rows.Count;
            return result;
        }

        private static async Task EnforceReadOnly(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            await using NpgsqlCommand command = new NpgsqlCommand("SET default_transaction_read_only = on", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task SetStatementTimeout(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            await using NpgsqlCommand command = new NpgsqlCommand("SET statement_timeout = '30s'", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<List<TableInfo>> GetTablesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            List<TableInfo> tables = new List<TableInfo>();

            string sql = @"
                SELECT t.table_schema, t.table_name, c.column_name, c.data_type,
                       c.is_nullable, c.column_default, c.ordinal_position,
                       c.udt_name
                FROM information_schema.tables t
                JOIN information_schema.columns c
                    ON t.table_schema = c.table_schema AND t.table_name = c.table_name
                WHERE t.table_type = 'BASE TABLE'
                    AND t.table_schema NOT IN ('pg_catalog', 'information_schema')
                ORDER BY t.table_schema, t.table_name, c.ordinal_position";

            await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            TableInfo? currentTable = null;

            while (await reader.ReadAsync(cancellationToken))
            {
                string schemaName = reader.GetString(0);
                string tableName = reader.GetString(1);

                if (currentTable == null || currentTable.SchemaName != schemaName || currentTable.TableName != tableName)
                {
                    currentTable = new TableInfo
                    {
                        SchemaName = schemaName,
                        TableName = tableName,
                    };
                    tables.Add(currentTable);
                }

                string dataType = reader.GetString(3);
                string udtName = reader.GetString(7);
                if (dataType == "USER-DEFINED")
                {
                    dataType = udtName;
                }
                else if (dataType == "ARRAY")
                {
                    dataType = udtName.TrimStart('_') + "[]";
                }

                currentTable.Columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(2),
                    DataType = dataType,
                    IsNullable = reader.GetString(4) == "YES",
                    ColumnDefault = reader.IsDBNull(5) ? null : reader.GetString(5),
                    OrdinalPosition = reader.GetInt32(6),
                });
            }

            return tables;
        }

        private static async Task<Dictionary<string, List<string>>> GetPrimaryKeysAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            Dictionary<string, List<string>> primaryKeys = new Dictionary<string, List<string>>();

            string sql = @"
                SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                    AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
                ORDER BY kcu.table_schema, kcu.table_name, kcu.ordinal_position";

            await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                string key = $"{reader.GetString(0)}.{reader.GetString(1)}";
                if (!primaryKeys.ContainsKey(key))
                {
                    primaryKeys[key] = new List<string>();
                }

                primaryKeys[key].Add(reader.GetString(2));
            }

            return primaryKeys;
        }

        private static async Task<Dictionary<string, List<ForeignKeyInfo>>> GetForeignKeysAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            Dictionary<string, List<ForeignKeyInfo>> foreignKeys = new Dictionary<string, List<ForeignKeyInfo>>();

            string sql = @"
                SELECT
                    tc.table_schema,
                    tc.table_name,
                    tc.constraint_name,
                    kcu.column_name,
                    ccu.table_name AS referenced_table,
                    ccu.column_name AS referenced_column
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu
                    ON tc.constraint_name = ccu.constraint_name
                    AND tc.table_schema = ccu.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                    AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
                ORDER BY tc.table_schema, tc.table_name, tc.constraint_name";

            await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                string key = $"{reader.GetString(0)}.{reader.GetString(1)}";
                if (!foreignKeys.ContainsKey(key))
                {
                    foreignKeys[key] = new List<ForeignKeyInfo>();
                }

                foreignKeys[key].Add(new ForeignKeyInfo
                {
                    ConstraintName = reader.GetString(2),
                    ColumnName = reader.GetString(3),
                    ReferencedTable = reader.GetString(4),
                    ReferencedColumn = reader.GetString(5),
                });
            }

            return foreignKeys;
        }
    }
}
