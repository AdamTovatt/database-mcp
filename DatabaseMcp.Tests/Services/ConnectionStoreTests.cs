using DatabaseMcp.Core.Models;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Tests.Services
{
    public class ConnectionStoreTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ConnectionStore _store;

        public ConnectionStoreTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"db-mcp-test-{Guid.NewGuid()}");
            _store = new ConnectionStore(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Fact]
        public void GetAll_EmptyStore_ReturnsEmptyList()
        {
            List<SavedConnection> result = _store.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void Add_ThenGetAll_ReturnsConnection()
        {
            SavedConnection connection = CreateTestConnection("test-db");

            _store.Add(connection);
            List<SavedConnection> result = _store.GetAll();

            Assert.Single(result);
            Assert.Equal("test-db", result[0].Name);
        }

        [Fact]
        public void Add_ThenGetByName_ReturnsConnection()
        {
            SavedConnection connection = CreateTestConnection("my-db");

            _store.Add(connection);
            SavedConnection? result = _store.GetByName("my-db");

            Assert.NotNull(result);
            Assert.Equal("my-db", result.Name);
            Assert.Equal("postgres", result.ProviderType);
            Assert.Equal("localhost", result.Metadata.Host);
        }

        [Fact]
        public void GetByName_NotFound_ReturnsNull()
        {
            SavedConnection? result = _store.GetByName("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public void GetByName_CaseInsensitive()
        {
            _store.Add(CreateTestConnection("MyDatabase"));

            SavedConnection? result = _store.GetByName("mydatabase");

            Assert.NotNull(result);
            Assert.Equal("MyDatabase", result.Name);
        }

        [Fact]
        public void Add_DuplicateName_ThrowsInvalidOperationException()
        {
            _store.Add(CreateTestConnection("duplicate"));

            Assert.Throws<InvalidOperationException>(() =>
                _store.Add(CreateTestConnection("duplicate")));
        }

        [Fact]
        public void Remove_ExistingConnection_ReturnsTrue()
        {
            _store.Add(CreateTestConnection("to-remove"));

            bool removed = _store.Remove("to-remove");

            Assert.True(removed);
            Assert.Null(_store.GetByName("to-remove"));
        }

        [Fact]
        public void Remove_NonExistent_ReturnsFalse()
        {
            bool removed = _store.Remove("nonexistent");

            Assert.False(removed);
        }

        [Fact]
        public void EncryptThenDecrypt_ReturnsOriginal()
        {
            string original = "Host=localhost;Password=secret";

            string encrypted = _store.EncryptConnectionString(original);
            string decrypted = _store.DecryptConnectionString(encrypted);

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Add_ConnectionString_IsEncryptedOnDisk()
        {
            string connectionString = "Host=localhost;Password=super-secret-password";
            string encrypted = _store.EncryptConnectionString(connectionString);

            SavedConnection connection = new SavedConnection
            {
                Name = "encrypted-test",
                ProviderType = "postgres",
                EncryptedConnectionString = encrypted,
                Metadata = new ConnectionMetadata("localhost", 5432, "testdb", "user"),
            };
            _store.Add(connection);

            string configPath = Path.Combine(_tempDir, "connections.json");
            string fileContents = File.ReadAllText(configPath);

            Assert.DoesNotContain("super-secret-password", fileContents);
            Assert.Contains("encrypted-test", fileContents);
        }

        [Fact]
        public void Add_MultipleConnections_AllPersisted()
        {
            _store.Add(CreateTestConnection("db1"));
            _store.Add(CreateTestConnection("db2"));
            _store.Add(CreateTestConnection("db3"));

            ConnectionStore freshStore = new ConnectionStore(_tempDir);
            List<SavedConnection> result = freshStore.GetAll();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Add_MetadataFieldsPreserved()
        {
            SavedConnection connection = new SavedConnection
            {
                Name = "metadata-test",
                ProviderType = "postgres",
                EncryptedConnectionString = _store.EncryptConnectionString("Host=db.example.com;Database=prod"),
                Metadata = new ConnectionMetadata("db.example.com", 5433, "prod", "admin"),
            };
            _store.Add(connection);

            ConnectionStore freshStore = new ConnectionStore(_tempDir);
            SavedConnection? loaded = freshStore.GetByName("metadata-test");

            Assert.NotNull(loaded);
            Assert.Equal("db.example.com", loaded.Metadata.Host);
            Assert.Equal(5433, loaded.Metadata.Port);
            Assert.Equal("prod", loaded.Metadata.Database);
            Assert.Equal("admin", loaded.Metadata.Username);
        }

        private SavedConnection CreateTestConnection(string name)
        {
            return new SavedConnection
            {
                Name = name,
                ProviderType = "postgres",
                EncryptedConnectionString = _store.EncryptConnectionString($"Host=localhost;Database={name}"),
                Metadata = new ConnectionMetadata("localhost", 5432, name, "testuser"),
            };
        }
    }
}
