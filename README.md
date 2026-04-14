# DatabaseMcp

[![Tests](https://github.com/AdamTovatt/database-mcp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AdamTovatt/database-mcp/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/DatabaseMcp.svg)](https://www.nuget.org/packages/DatabaseMcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

A read-only database query tool for AI agents and CLI users. Works as both a CLI tool and an MCP (Model Context Protocol) server. Designed to be safe for production databases — all connections are enforced read-only at the database session level.

## Installation

### Install script (macOS / Linux)

```bash
curl -fsSL https://raw.githubusercontent.com/AdamTovatt/database-mcp/master/install-db.sh | bash
```

This detects your platform, downloads the latest release, and installs the `db` binary to `/usr/local/bin`. Requires the [GitHub CLI](https://cli.github.com/) (`gh`) to be installed and authenticated. Run the same command again to update.

### Updaemon (Linux)

If you use [updaemon](https://github.com/AdamTovatt/updaemon) (v0.8.0+) for managing tools:

```bash
updaemon new db --from github --remote AdamTovatt/database-mcp/db-linux-arm64.zip --type cli
updaemon set-exec-name db DatabaseMcp.Cli
updaemon init db
```

Replace `db-linux-arm64.zip` with `db-linux-x64.zip` on x86_64 systems. Future updates are handled by `updaemon update`.

### .NET tool

```bash
dotnet tool install --global DatabaseMcp
```

After installation, the `db` command will be available globally.

To update:

```bash
dotnet tool update --global DatabaseMcp
```

To uninstall:

```bash
dotnet tool uninstall --global DatabaseMcp
```

To register it as an MCP tool in Claude Code:

```bash
claude mcp add database -- db --mcp
```

For Cursor or other MCP clients, add this to your MCP configuration:

```json
{
  "mcpServers": {
    "database": {
      "command": "db",
      "args": ["--mcp"]
    }
  }
}
```

## Usage

```bash
db add <name> <connection-string>   # Save a named database connection
db remove <name>                    # Remove a saved connection
db list                             # List all saved connections
db test [name]                      # Test a database connection
db schema [name]                    # Show database schema
db query [name] <sql>               # Execute a read-only SQL query
db help                             # Show help information
```

### Connection String Formats

All common PostgreSQL formats are supported:

```bash
# URI format
db add production "postgres://user:pass@host:5432/database"
db add staging "postgresql://user:pass@host/database?sslmode=require"

# Standard ADO.NET format
db add local "Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass"
```

The provider type is auto-detected from the connection string format.

### Examples

```bash
# Add connections
db add production "postgres://admin:secret@db.prod.com:5432/myapp"
db add local "Host=localhost;Database=myapp_dev;Username=postgres;Password=postgres"

# List saved connections (never shows passwords)
db list

# Test that a connection works
db test production

# View the database schema
db schema production

# Run queries (read-only enforced)
db query production "SELECT count(*) FROM users"
db query local "SELECT * FROM orders WHERE created_at > '2025-01-01' LIMIT 10"

# If you omit the connection name, you'll be prompted to pick one
db schema
db test
```

## Behavior

### Read-only enforcement

All database connections execute `SET default_transaction_read_only = on` at the session level immediately after connecting. This means PostgreSQL itself rejects any `INSERT`, `UPDATE`, `DELETE`, `CREATE`, `DROP`, `ALTER`, or other write operations. This cannot be bypassed by the SQL query — it is enforced by the database engine.

### Statement timeout

A 30-second statement timeout is applied to all queries to prevent runaway queries from blocking the connection.

### Row limit

Query results are capped at 1000 rows. If a query returns more, the output indicates the results were truncated.

### Encrypted connection storage

Connection strings are stored encrypted (AES-256) in `~/.config/database-mcp/connections.json`. Display metadata (host, database, username) is stored in plaintext alongside the encrypted connection string so that `db list` and MCP tools can show connection info without decrypting.

> **Note on encryption:** The encryption key is derived from a value compiled into the binary. This is **not** a cryptographic security measure — it is accident prevention. It protects against LLMs reading plaintext credentials from the config file, automated credential scanners, and casual file exposure. For production secrets management, use a proper secrets vault. Anyone with access to the source code could derive the key.

### Config file permissions

On Unix systems, the config file is created with permissions `600` (owner read/write only).

### Provider abstraction

The tool is designed to support multiple database providers. Currently supported:

- **PostgreSQL** via Npgsql

The provider type is stored per connection, so adding support for additional databases (SQLite, SQL Server, etc.) requires only a new provider implementation.

## As MCP Server

```bash
db --mcp
```

When running as an MCP server, the following tools are available:

- `db_list_connections()` — List saved connections with safe metadata (never exposes passwords or connection strings)
- `db_schema(connectionName)` — Get the full database schema (tables, columns, types, primary keys, foreign keys)
- `db_query(connectionName, sql)` — Execute a read-only SQL query (max 1000 rows)

## Development

```bash
git clone <repository-url>
cd database-mcp
dotnet build DatabaseMcp.slnx
dotnet test DatabaseMcp.slnx
```

To run as MCP server during development:

```bash
dotnet run --project DatabaseMcp.Cli/DatabaseMcp.Cli.csproj -- --mcp
```

To package:

```bash
dotnet pack DatabaseMcp.Cli/DatabaseMcp.Cli.csproj --configuration Release
```

## License

MIT License
