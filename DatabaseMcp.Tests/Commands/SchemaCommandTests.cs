using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using Moq;

namespace DatabaseMcp.Tests.Commands
{
    public class SchemaCommandTests
    {
        private readonly Mock<IConnectionStore> _storeMock = new Mock<IConnectionStore>();
        private readonly Mock<IDatabaseProviderFactory> _factoryMock = new Mock<IDatabaseProviderFactory>();
        private readonly Mock<IDatabaseProvider> _providerMock = new Mock<IDatabaseProvider>();

        public SchemaCommandTests()
        {
            _factoryMock.Setup(f => f.Create("postgres")).Returns(_providerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ConnectionNotFound_ReturnsFailure()
        {
            _storeMock.Setup(s => s.GetByName("missing")).Returns((SavedConnection?)null);

            SchemaCommand command = new SchemaCommand(_storeMock.Object, _factoryMock.Object, "missing");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ValidSchema_ReturnsFormattedOutput()
        {
            SetupConnection("my-db");
            SchemaInfo schema = new SchemaInfo
            {
                Tables = new List<TableInfo>
                {
                    new TableInfo
                    {
                        SchemaName = "public",
                        TableName = "users",
                        Columns = new List<ColumnInfo>
                        {
                            new ColumnInfo { Name = "id", DataType = "integer", IsNullable = false, IsPrimaryKey = true, OrdinalPosition = 1 },
                            new ColumnInfo { Name = "email", DataType = "text", IsNullable = false, IsPrimaryKey = false, OrdinalPosition = 2 },
                            new ColumnInfo { Name = "name", DataType = "text", IsNullable = true, IsPrimaryKey = false, OrdinalPosition = 3 },
                        },
                    },
                },
            };
            _providerMock.Setup(p => p.GetSchemaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(schema);

            SchemaCommand command = new SchemaCommand(_storeMock.Object, _factoryMock.Object, "my-db");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("1 table", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("public.users", result.Details);
            Assert.Contains("id", result.Details);
            Assert.Contains("[PK]", result.Details);
            Assert.Contains("email", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_SchemaWithForeignKeys_IncludesFkInfo()
        {
            SetupConnection("fk-db");
            SchemaInfo schema = new SchemaInfo
            {
                Tables = new List<TableInfo>
                {
                    new TableInfo
                    {
                        SchemaName = "public",
                        TableName = "orders",
                        Columns = new List<ColumnInfo>
                        {
                            new ColumnInfo { Name = "id", DataType = "integer", IsNullable = false, IsPrimaryKey = true, OrdinalPosition = 1 },
                            new ColumnInfo { Name = "user_id", DataType = "integer", IsNullable = false, IsPrimaryKey = false, OrdinalPosition = 2 },
                        },
                        ForeignKeys = new List<ForeignKeyInfo>
                        {
                            new ForeignKeyInfo
                            {
                                ConstraintName = "fk_orders_user",
                                ColumnName = "user_id",
                                ReferencedTable = "users",
                                ReferencedColumn = "id",
                            },
                        },
                    },
                },
            };
            _providerMock.Setup(p => p.GetSchemaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(schema);

            SchemaCommand command = new SchemaCommand(_storeMock.Object, _factoryMock.Object, "fk-db");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.NotNull(result.Details);
            Assert.Contains("Foreign keys", result.Details);
            Assert.Contains("user_id -> users.id", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ProviderError_ReturnsFailure()
        {
            SetupConnection("error-db");
            _providerMock.Setup(p => p.GetSchemaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Permission denied"));

            SchemaCommand command = new SchemaCommand(_storeMock.Object, _factoryMock.Object, "error-db");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Permission denied", result.Message);
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
