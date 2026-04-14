using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Models;
using Moq;

namespace DatabaseMcp.Tests.Commands
{
    public class CommandFactoryTests
    {
        private readonly Mock<IConnectionStore> _storeMock = new Mock<IConnectionStore>();
        private readonly Mock<IDatabaseProviderFactory> _factoryMock = new Mock<IDatabaseProviderFactory>();
        private readonly CommandFactory _commandFactory;

        public CommandFactoryTests()
        {
            _commandFactory = new CommandFactory(_storeMock.Object, _factoryMock.Object);
        }

        [Fact]
        public void CreateCommand_NoArgs_ReturnsHelpCommand()
        {
            ICommand command = _commandFactory.CreateCommand(Array.Empty<string>());

            Assert.IsType<HelpCommand>(command);
        }

        [Theory]
        [InlineData("help")]
        [InlineData("--help")]
        [InlineData("-h")]
        public void CreateCommand_HelpVariants_ReturnsHelpCommand(string arg)
        {
            ICommand command = _commandFactory.CreateCommand(new[] { arg });

            Assert.IsType<HelpCommand>(command);
        }

        [Fact]
        public void CreateCommand_Add_WithArgs_ReturnsAddCommand()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "add", "mydb", "Host=localhost;Database=test" });

            Assert.IsType<AddCommand>(command);
        }

        [Fact]
        public void CreateCommand_Add_MissingArgs_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _commandFactory.CreateCommand(new[] { "add" }));
        }

        [Fact]
        public void CreateCommand_Add_MissingConnectionString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _commandFactory.CreateCommand(new[] { "add", "mydb" }));
        }

        [Fact]
        public void CreateCommand_Remove_WithName_ReturnsRemoveCommand()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "remove", "mydb" });

            Assert.IsType<RemoveCommand>(command);
        }

        [Fact]
        public void CreateCommand_Remove_MissingName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _commandFactory.CreateCommand(new[] { "remove" }));
        }

        [Fact]
        public void CreateCommand_List_ReturnsListCommand()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "list" });

            Assert.IsType<ListCommand>(command);
        }

        [Fact]
        public void CreateCommand_Test_WithName_ReturnsTestCommand()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "test", "mydb" });

            Assert.IsType<TestCommand>(command);
        }

        [Fact]
        public void CreateCommand_Schema_WithName_ReturnsSchemaCommand()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "schema", "mydb" });

            Assert.IsType<SchemaCommand>(command);
        }

        [Fact]
        public void CreateCommand_Query_WithNameAndSql_ReturnsQueryCommand()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "query", "mydb", "SELECT 1" });

            Assert.IsType<QueryCommand>(command);
        }

        [Fact]
        public void CreateCommand_Query_MissingArgs_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _commandFactory.CreateCommand(new[] { "query" }));
        }

        [Fact]
        public void CreateCommand_Query_MissingSql_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _commandFactory.CreateCommand(new[] { "query", "mydb" }));
        }

        [Fact]
        public void CreateCommand_UnknownCommand_ThrowsArgumentException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                _commandFactory.CreateCommand(new[] { "foobar" }));

            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void CreateCommand_Query_JoinsMultipleArgsAsSql()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "query", "mydb", "SELECT", "*", "FROM", "users" });

            Assert.IsType<QueryCommand>(command);
        }

        [Fact]
        public void CreateCommand_Add_JoinsConnectionStringArgs()
        {
            ICommand command = _commandFactory.CreateCommand(new[] { "add", "mydb", "Host=localhost;", "Database=test" });

            Assert.IsType<AddCommand>(command);
        }
    }
}
