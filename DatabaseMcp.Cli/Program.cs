using DatabaseMcp.Core.Commands;
using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DatabaseMcp.Cli
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--version", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(typeof(Program).Assembly.GetName().Version);
                return 0;
            }

            bool isMcpMode = args.Length > 0 &&
                (args[0].Equals("--mcp", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("mcp", StringComparison.OrdinalIgnoreCase));

            if (isMcpMode)
            {
                return await RunMcpServerAsync(args);
            }
            else
            {
                return await RunCliAsync(args);
            }
        }

        private static async Task<int> RunCliAsync(string[] args)
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            try
            {
                ICommandFactory commandFactory = serviceProvider.GetRequiredService<ICommandFactory>();
                ICommand command = commandFactory.CreateCommand(args);
                CommandResult result = await command.ExecuteAsync(CancellationToken.None);

                Console.WriteLine(result.Message);
                if (!string.IsNullOrEmpty(result.Details))
                {
                    Console.WriteLine(result.Details);
                }

                return result.Success ? 0 : 1;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunMcpServerAsync(string[] args)
        {
            try
            {
                HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

                builder.Logging.ClearProviders();
                builder.Logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Trace;
                });

                ConfigureServices(builder.Services);

                builder.Services
                    .AddMcpServer()
                    .WithStdioServerTransport()
                    .WithToolsFromAssembly();

                builder.Services.AddSingleton<McpTools>();

                IHost host = builder.Build();
                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"MCP Server error: {ex.Message}");
                return 1;
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionStore, ConnectionStore>();
            services.AddSingleton<IDatabaseProviderFactory, DatabaseProviderFactory>();
            services.AddSingleton<ICommandFactory, CommandFactory>();
        }
    }
}
