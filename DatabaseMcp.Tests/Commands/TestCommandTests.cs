using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using Moq;

namespace DatabaseMcp.Tests.Commands
{
    public class TestCommandTests
    {
        private readonly Mock<IConnectionStore> _storeMock = new Mock<IConnectionStore>();
        private readonly Mock<IDatabaseProviderFactory> _factoryMock = new Mock<IDatabaseProviderFactory>();
        private readonly Mock<IDatabaseProvider> _providerMock = new Mock<IDatabaseProvider>();

        public TestCommandTests()
        {
            _factoryMock.Setup(f => f.Create("postgres")).Returns(_providerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ConnectionNotFound_ReturnsFailure()
        {
            _storeMock.Setup(s => s.GetByName("missing")).Returns((SavedConnection?)null);

            TestCommand command = new TestCommand(_storeMock.Object, _factoryMock.Object, "missing");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ConnectionSucceeds_ReturnsSuccess()
        {
            SetupConnection("working-db");
            _providerMock.Setup(p => p.TestConnectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            TestCommand command = new TestCommand(_storeMock.Object, _factoryMock.Object, "working-db");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("working", result.Message);
            Assert.DoesNotContain("localhost", result.Message);
            Assert.DoesNotContain("testdb", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ConnectionFails_ReturnsFailure()
        {
            SetupConnection("broken-db");
            _providerMock.Setup(p => p.TestConnectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection refused"));

            TestCommand command = new TestCommand(_storeMock.Object, _factoryMock.Object, "broken-db");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Connection refused", result.Message);
        }

        private void SetupConnection(string name)
        {
            _storeMock.Setup(s => s.GetByName(name)).Returns(new SavedConnection
            {
                Name = name,
                ProviderType = "postgres",
                EncryptedConnectionString = "encrypted-value",
                Metadata = new ConnectionMetadata("localhost", 5432, "testdb", "user"),
            });
            _storeMock.Setup(s => s.DecryptConnectionString("encrypted-value")).Returns("Host=localhost;Database=testdb");
        }
    }
}
