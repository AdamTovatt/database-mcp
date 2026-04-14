namespace DatabaseMcp.Core.Models
{
    /// <summary>
    /// The result of executing a SQL query.
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// The column names in the result set.
        /// </summary>
        public List<string> Columns { get; set; } = new();

        /// <summary>
        /// The result rows, each represented as a mapping of column name to value.
        /// </summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        /// <summary>
        /// The number of rows returned (before truncation).
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Whether the result set was truncated due to exceeding the maximum row limit.
        /// </summary>
        public bool Truncated { get; set; }
    }
}
