namespace DatabaseMcp.Core.Models
{
    /// <summary>
    /// Complete database schema information.
    /// </summary>
    public class SchemaInfo
    {
        /// <summary>
        /// All tables in the database.
        /// </summary>
        public List<TableInfo> Tables { get; set; } = new();
    }

    /// <summary>
    /// Information about a single database table.
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// The schema name (e.g. "public").
        /// </summary>
        public required string SchemaName { get; set; }

        /// <summary>
        /// The table name.
        /// </summary>
        public required string TableName { get; set; }

        /// <summary>
        /// The columns in this table.
        /// </summary>
        public List<ColumnInfo> Columns { get; set; } = new();

        /// <summary>
        /// The foreign key constraints on this table.
        /// </summary>
        public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
    }

    /// <summary>
    /// Information about a single column in a table.
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// The column name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The column data type (e.g. "integer", "text", "timestamp with time zone").
        /// </summary>
        public required string DataType { get; set; }

        /// <summary>
        /// Whether the column allows null values.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// The column default value expression, or null if none.
        /// </summary>
        public string? ColumnDefault { get; set; }

        /// <summary>
        /// Whether this column is part of the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// The ordinal position of the column in the table.
        /// </summary>
        public int OrdinalPosition { get; set; }
    }

    /// <summary>
    /// Information about a foreign key constraint.
    /// </summary>
    public class ForeignKeyInfo
    {
        /// <summary>
        /// The constraint name.
        /// </summary>
        public required string ConstraintName { get; set; }

        /// <summary>
        /// The column name in the referencing table.
        /// </summary>
        public required string ColumnName { get; set; }

        /// <summary>
        /// The referenced table name.
        /// </summary>
        public required string ReferencedTable { get; set; }

        /// <summary>
        /// The referenced column name.
        /// </summary>
        public required string ReferencedColumn { get; set; }
    }
}
