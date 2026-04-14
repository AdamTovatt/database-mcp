using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Services
{
    /// <summary>
    /// Parses PostgreSQL connection strings in all common formats and extracts display-safe metadata.
    /// </summary>
    public static class ConnectionStringParser
    {
        /// <summary>
        /// Parses a connection string and returns the normalized connection string, display metadata, and detected provider type.
        /// </summary>
        /// <param name="connectionString">The connection string in any supported format.</param>
        /// <returns>A tuple of (normalizedConnectionString, metadata, providerType).</returns>
        /// <exception cref="ArgumentException">Thrown if the connection string cannot be parsed.</exception>
        public static (string NormalizedConnectionString, ConnectionMetadata Metadata, string ProviderType) Parse(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be empty.");
            }

            string trimmed = connectionString.Trim();

            if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return ParseUri(trimmed);
            }

            return ParseStandard(trimmed);
        }

        private static (string NormalizedConnectionString, ConnectionMetadata Metadata, string ProviderType) ParseUri(string uriString)
        {
            Uri uri;
            try
            {
                uri = new Uri(uriString);
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException($"Invalid connection URI: {ex.Message}", ex);
            }

            string host = uri.Host;
            int port = uri.Port > 0 ? uri.Port : 5432;
            string database = uri.AbsolutePath.TrimStart('/');
            string username = "";
            string password = "";

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] userInfoParts = uri.UserInfo.Split(':', 2);
                username = Uri.UnescapeDataString(userInfoParts[0]);
                if (userInfoParts.Length > 1)
                {
                    password = Uri.UnescapeDataString(userInfoParts[1]);
                }
            }

            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Connection URI must include a host.");
            }

            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("Connection URI must include a database name.");
            }

            List<string> parts = new List<string>
            {
                $"Host={host}",
                $"Port={port}",
                $"Database={database}",
            };

            if (!string.IsNullOrEmpty(username))
            {
                parts.Add($"Username={username}");
            }

            if (!string.IsNullOrEmpty(password))
            {
                parts.Add($"Password={password}");
            }

            string? query = uri.Query?.TrimStart('?');
            if (!string.IsNullOrEmpty(query))
            {
                foreach (string param in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] kv = param.Split('=', 2);
                    if (kv.Length == 2)
                    {
                        string key = MapUriParamToNpgsql(Uri.UnescapeDataString(kv[0]));
                        string value = Uri.UnescapeDataString(kv[1]);
                        parts.Add($"{key}={value}");
                    }
                }
            }

            string normalized = string.Join(";", parts);
            ConnectionMetadata metadata = new ConnectionMetadata(host, port, database, username);
            return (normalized, metadata, "postgres");
        }

        private static (string NormalizedConnectionString, ConnectionMetadata Metadata, string ProviderType) ParseStandard(string connectionString)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = part.Split('=', 2);
                if (kv.Length == 2)
                {
                    pairs[kv[0].Trim()] = kv[1].Trim();
                }
            }

            string host = GetValueOrDefault(pairs, new[] { "Host", "Server" }, "");
            string portStr = GetValueOrDefault(pairs, new[] { "Port" }, "5432");
            string database = GetValueOrDefault(pairs, new[] { "Database", "Initial Catalog" }, "");
            string username = GetValueOrDefault(pairs, new[] { "Username", "User Id", "User" }, "");

            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Connection string must include a Host.");
            }

            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("Connection string must include a Database.");
            }

            if (!int.TryParse(portStr, out int port))
            {
                port = 5432;
            }

            ConnectionMetadata metadata = new ConnectionMetadata(host, port, database, username);
            return (connectionString, metadata, "postgres");
        }

        private static string GetValueOrDefault(Dictionary<string, string> pairs, string[] keys, string defaultValue)
        {
            foreach (string key in keys)
            {
                if (pairs.TryGetValue(key, out string? value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static string MapUriParamToNpgsql(string param)
        {
            return param.ToLowerInvariant() switch
            {
                "sslmode" => "SSL Mode",
                "connect_timeout" => "Timeout",
                "application_name" => "Application Name",
                _ => param,
            };
        }
    }
}
