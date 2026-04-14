using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using Moq;

namespace DatabaseMcp.Tests.Commands
{
    public class QueryCommandTests
    {
        private readonly Mock<IConnectionStore> _storeMock = new Mock<IConnectionStore>();
        private readonly Mock<IDatabaseProviderFactory> _factoryMock = new Mock<IDatabaseProviderFactory>();
        private readonly Mock<IDatabaseProvider> _providerMock = new Mock<IDatabaseProvider>();

        public QueryCommandTests()
        {
            _factoryMock.Setup(f => f.Create("postgres")).Returns(_providerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ConnectionNotFound_ReturnsFailure()
        {
            _storeMock.Setup(s => s.GetByName("missing")).Returns((SavedConnection?)null);

            QueryCommand command = new QueryCommand(_storeMock.Object, _factoryMock.Object, "missing", "SELECT 1");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ResultsReturned_FormatsAsTable()
        {
            SetupConnection("my-db");
            QueryResult queryResult = new QueryResult
            {
                Columns = new List<string> { "id", "name" },
                Rows = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "id", 1 }, { "name", "Alice" } },
                    new Dictionary<string, object?> { { "id", 2 }, { "name", "Bob" } },
                },
                RowCount = 2,
                Truncated = false,
            };
            _providerMock.Setup(p => p.ExecuteQueryAsync(It.IsAny<string>(), "SELECT * FROM users", 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            QueryCommand command = new QueryCommand(_storeMock.Object, _factoryMock.Object, "my-db", "SELECT * FROM users");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("2 row(s)", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("Alice", result.Details);
            Assert.Contains("Bob", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_TruncatedResults_IndicatesTruncation()
        {
            SetupConnection("big-db");
            QueryResult queryResult = new QueryResult
            {
                Columns = new List<string> { "id" },
                Rows = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "id", 1 } },
                },
                RowCount = 1,
                Truncated = true,
            };
            _providerMock.Setup(p => p.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            QueryCommand command = new QueryCommand(_storeMock.Object, _factoryMock.Object, "big-db", "SELECT * FROM big_table");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.Contains("truncated", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_NoColumns_ReturnsAppropriateMessage()
        {
            SetupConnection("no-col-db");
            QueryResult queryResult = new QueryResult
            {
                Columns = new List<string>(),
                Rows = new List<Dictionary<string, object?>>(),
                RowCount = 0,
                Truncated = false,
            };
            _providerMock.Setup(p => p.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            QueryCommand command = new QueryCommand(_storeMock.Object, _factoryMock.Object, "no-col-db", "DO $$BEGIN END$$");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No columns", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_NullValues_FormattedAsNULL()
        {
            SetupConnection("null-db");
            QueryResult queryResult = new QueryResult
            {
                Columns = new List<string> { "id", "name" },
                Rows = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "id", 1 }, { "name", null } },
                },
                RowCount = 1,
                Truncated = false,
            };
            _providerMock.Setup(p => p.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            QueryCommand command = new QueryCommand(_storeMock.Object, _factoryMock.Object, "null-db", "SELECT * FROM t");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.NotNull(result.Details);
            Assert.Contains("NULL", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ProviderError_ReturnsFailure()
        {
            SetupConnection("error-db");
            _providerMock.Setup(p => p.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("cannot execute INSERT in a read-only transaction"));

            QueryCommand command = new QueryCommand(_storeMock.Object, _factoryMock.Object, "error-db", "INSERT INTO t VALUES (1)");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("read-only", result.Message);
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
