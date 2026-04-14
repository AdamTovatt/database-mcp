using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Tests.Commands
{
    public class AddCommandTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ConnectionStore _store;

        public AddCommandTests()
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
        public async Task ExecuteAsync_ValidStandardFormat_AddsSuccessfully()
        {
            AddCommand command = new AddCommand(_store, "test-db", "Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass");

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("test-db", result.Message);
            Assert.DoesNotContain("localhost", result.Message);
            Assert.NotNull(_store.GetByName("test-db"));
        }

        [Fact]
        public async Task ExecuteAsync_ValidUriFormat_AddsSuccessfully()
        {
            AddCommand command = new AddCommand(_store, "uri-db", "postgres://user:pass@host:5432/database");

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            SavedConnection? saved = _store.GetByName("uri-db");
            Assert.NotNull(saved);
            Assert.Equal("host", saved.Metadata.Host);
            Assert.Equal("database", saved.Metadata.Database);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateName_ReturnsFailure()
        {
            AddCommand first = new AddCommand(_store, "dup", "Host=localhost;Database=db1;Username=user;Password=pass");
            await first.ExecuteAsync(CancellationToken.None);

            AddCommand second = new AddCommand(_store, "dup", "Host=localhost;Database=db2;Username=user;Password=pass");
            CommandResult result = await second.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("already exists", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_InvalidConnectionString_ReturnsFailure()
        {
            AddCommand command = new AddCommand(_store, "bad", "not-a-connection-string");

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Invalid connection string", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ConnectionStringIsEncrypted()
        {
            AddCommand command = new AddCommand(_store, "enc-test", "Host=localhost;Database=mydb;Username=user;Password=supersecret");
            await command.ExecuteAsync(CancellationToken.None);

            SavedConnection? saved = _store.GetByName("enc-test");
            Assert.NotNull(saved);
            Assert.DoesNotContain("supersecret", saved.EncryptedConnectionString);

            string decrypted = _store.DecryptConnectionString(saved.EncryptedConnectionString);
            Assert.Contains("supersecret", decrypted);
        }
    }
}
