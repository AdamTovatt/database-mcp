using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Tests.Commands
{
    public class RemoveCommandTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ConnectionStore _store;

        public RemoveCommandTests()
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
        public async Task ExecuteAsync_ExistingConnection_RemovesSuccessfully()
        {
            _store.Add(new SavedConnection
            {
                Name = "to-remove",
                ProviderType = "postgres",
                EncryptedConnectionString = _store.EncryptConnectionString("Host=localhost;Database=db"),
                Metadata = new ConnectionMetadata("localhost", 5432, "db", "user"),
            });

            RemoveCommand command = new RemoveCommand(_store, "to-remove");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("removed", result.Message);
            Assert.Null(_store.GetByName("to-remove"));
        }

        [Fact]
        public async Task ExecuteAsync_NonExistent_ReturnsFailure()
        {
            RemoveCommand command = new RemoveCommand(_store, "nonexistent");

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }
    }
}
