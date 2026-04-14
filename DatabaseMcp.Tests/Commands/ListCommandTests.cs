using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Tests.Commands
{
    public class ListCommandTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ConnectionStore _store;

        public ListCommandTests()
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
        public async Task ExecuteAsync_NoConnections_ReturnsHelpfulMessage()
        {
            ListCommand command = new ListCommand(_store);

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No connections saved", result.Message);
            Assert.Contains("db add", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithConnections_ShowsAllFields()
        {
            _store.Add(new SavedConnection
            {
                Name = "production",
                ProviderType = "postgres",
                EncryptedConnectionString = _store.EncryptConnectionString("Host=db.prod.com;Database=app;Password=secret"),
                Metadata = new ConnectionMetadata("db.prod.com", 5432, "app", "admin"),
            });

            ListCommand command = new ListCommand(_store);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("1 saved connection", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("production", result.Details);
            Assert.Contains("postgres", result.Details);
            Assert.Contains("db.prod.com", result.Details);
            Assert.Contains("app", result.Details);
            Assert.Contains("admin", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_NeverExposesPassword()
        {
            _store.Add(new SavedConnection
            {
                Name = "secure-db",
                ProviderType = "postgres",
                EncryptedConnectionString = _store.EncryptConnectionString("Host=localhost;Database=db;Password=my-secret-pass"),
                Metadata = new ConnectionMetadata("localhost", 5432, "db", "user"),
            });

            ListCommand command = new ListCommand(_store);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.DoesNotContain("my-secret-pass", result.Message);
            Assert.DoesNotContain("my-secret-pass", result.Details ?? "");
        }

        [Fact]
        public async Task ExecuteAsync_MultipleConnections_ListsAll()
        {
            _store.Add(CreateConnection("dev", "dev-host", "dev-db"));
            _store.Add(CreateConnection("staging", "staging-host", "staging-db"));
            _store.Add(CreateConnection("prod", "prod-host", "prod-db"));

            ListCommand command = new ListCommand(_store);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.Contains("3 saved connection", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("dev", result.Details);
            Assert.Contains("staging", result.Details);
            Assert.Contains("prod", result.Details);
        }

        private SavedConnection CreateConnection(string name, string host, string database)
        {
            return new SavedConnection
            {
                Name = name,
                ProviderType = "postgres",
                EncryptedConnectionString = _store.EncryptConnectionString($"Host={host};Database={database}"),
                Metadata = new ConnectionMetadata(host, 5432, database, "user"),
            };
        }
    }
}
