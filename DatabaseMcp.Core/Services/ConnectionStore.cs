using System.Text.Json;
using System.Text.Json.Serialization;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;

namespace DatabaseMcp.Core.Services
{
    /// <summary>
    /// Manages persistent storage of named database connections in an encrypted JSON config file.
    /// Connection strings are AES-256 encrypted; display metadata is stored in plaintext.
    /// </summary>
    public class ConnectionStore : IConnectionStore
    {
        private readonly string _configFilePath;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Creates a new connection store using the default config directory (~/.config/database-mcp/).
        /// </summary>
        public ConnectionStore()
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "database-mcp"))
        {
        }

        /// <summary>
        /// Creates a new connection store using a custom config directory (for testing).
        /// </summary>
        /// <param name="configDirectory">The directory to store the config file in.</param>
        public ConnectionStore(string configDirectory)
        {
            _configFilePath = Path.Combine(configDirectory, "connections.json");
        }

        /// <inheritdoc/>
        public List<SavedConnection> GetAll()
        {
            ConnectionConfig config = LoadConfig();
            return config.Connections;
        }

        /// <inheritdoc/>
        public SavedConnection? GetByName(string name)
        {
            List<SavedConnection> connections = GetAll();
            return connections.Find(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public void Add(SavedConnection connection)
        {
            ConnectionConfig config = LoadConfig();

            if (config.Connections.Any(c => c.Name.Equals(connection.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A connection named '{connection.Name}' already exists.");
            }

            config.Connections.Add(connection);
            SaveConfig(config);
        }

        /// <inheritdoc/>
        public bool Remove(string name)
        {
            ConnectionConfig config = LoadConfig();
            int removed = config.Connections.RemoveAll(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (removed == 0)
            {
                return false;
            }

            SaveConfig(config);
            return true;
        }

        /// <inheritdoc/>
        public string EncryptConnectionString(string connectionString)
        {
            return EncryptionService.Encrypt(connectionString);
        }

        /// <inheritdoc/>
        public string DecryptConnectionString(string encryptedConnectionString)
        {
            return EncryptionService.Decrypt(encryptedConnectionString);
        }

        private ConnectionConfig LoadConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                return new ConnectionConfig();
            }

            try
            {
                string json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<ConnectionConfig>(json, JsonOptions) ?? new ConnectionConfig();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(
                    $"Config file is corrupt: {_configFilePath}. " +
                    "Delete it and re-add your connections, or fix the JSON manually.");
            }
        }

        private void SaveConfig(ConnectionConfig config)
        {
            string? directory = Path.GetDirectoryName(_configFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configFilePath, json);

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_configFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }

        private class ConnectionConfig
        {
            public List<SavedConnection> Connections { get; set; } = new();
        }
    }
}
