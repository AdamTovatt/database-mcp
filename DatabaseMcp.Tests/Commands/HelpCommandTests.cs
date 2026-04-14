using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;

namespace DatabaseMcp.Tests.Commands
{
    public class HelpCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_ReturnsSuccess()
        {
            HelpCommand command = new HelpCommand();

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task ExecuteAsync_ContainsUsageInfo()
        {
            HelpCommand command = new HelpCommand();

            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.NotNull(result.Details);
            Assert.Contains("add", result.Details);
            Assert.Contains("remove", result.Details);
            Assert.Contains("list", result.Details);
            Assert.Contains("test", result.Details);
            Assert.Contains("schema", result.Details);
            Assert.Contains("query", result.Details);
            Assert.Contains("--mcp", result.Details);
        }
    }
}
