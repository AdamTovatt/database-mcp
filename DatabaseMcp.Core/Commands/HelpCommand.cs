using DatabaseMcp.Core.Interfaces;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Displays usage information.
    /// </summary>
    public class HelpCommand : ICommand
    {
        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            string message = "db — Read-only database query tool";
            string details = @"Usage: db <command> [arguments]

Commands:
  add <name> <connection-string>   Add a named database connection
  remove <name>                    Remove a saved connection
  list                             List all saved connections
  test [name]                      Test a database connection
  schema [name]                    Show database schema
  query [name] <sql>               Execute a read-only SQL query
  help                             Show this help message

Connection string formats:
  postgres://user:pass@host:5432/database
  postgresql://user:pass@host/database?sslmode=require
  Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass

MCP server mode:
  db --mcp                         Run as an MCP server (stdio transport)

All database connections are enforced read-only at the session level.
Connection strings are stored encrypted in ~/.config/database-mcp/connections.json.";

            return Task.FromResult(new CommandResult(true, message, details));
        }
    }
}
