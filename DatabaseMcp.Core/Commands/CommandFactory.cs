using DatabaseMcp.Core.Interfaces;

namespace DatabaseMcp.Core.Commands
{
    /// <summary>
    /// Creates command instances from CLI arguments.
    /// </summary>
    public class CommandFactory : ICommandFactory
    {
        private readonly IConnectionStore _store;
        private readonly IDatabaseProviderFactory _providerFactory;

        /// <summary>
        /// Creates a new command factory.
        /// </summary>
        /// <param name="store">The connection store.</param>
        /// <param name="providerFactory">The database provider factory.</param>
        public CommandFactory(IConnectionStore store, IDatabaseProviderFactory providerFactory)
        {
            _store = store;
            _providerFactory = providerFactory;
        }

        /// <inheritdoc/>
        public ICommand CreateCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return new HelpCommand();
            }

            string command = args[0].ToLowerInvariant();
            return command switch
            {
                "add" => CreateAddCommand(args),
                "remove" => CreateRemoveCommand(args),
                "list" => CreateListCommand(args),
                "test" => CreateTestCommand(args),
                "schema" => CreateSchemaCommand(args),
                "query" => CreateQueryCommand(args),
                "help" or "--help" or "-h" => new HelpCommand(),
                _ => throw new ArgumentException($"Unknown command '{args[0]}'. Run 'db help' for usage."),
            };
        }

        private AddCommand CreateAddCommand(string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Usage: db add <name> <connection-string>");
            }

            string name = args[1];
            string connectionString = string.Join(" ", args.Skip(2));
            return new AddCommand(_store, name, connectionString);
        }

        private RemoveCommand CreateRemoveCommand(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Usage: db remove <name>");
            }

            return new RemoveCommand(_store, args[1]);
        }

        private ListCommand CreateListCommand(string[] args)
        {
            bool includeDetails = args.Any(a => a.Equals("--details", StringComparison.OrdinalIgnoreCase) ||
                                                a.Equals("-d", StringComparison.OrdinalIgnoreCase));
            return new ListCommand(_store, includeDetails);
        }

        private TestCommand CreateTestCommand(string[] args)
        {
            if (args.Length < 2)
            {
                return CreateInteractiveTestCommand();
            }

            return new TestCommand(_store, _providerFactory, args[1]);
        }

        private SchemaCommand CreateSchemaCommand(string[] args)
        {
            if (args.Length < 2)
            {
                return CreateInteractiveSchemaCommand();
            }

            return new SchemaCommand(_store, _providerFactory, args[1]);
        }

        private QueryCommand CreateQueryCommand(string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Usage: db query <name> <sql>");
            }

            string name = args[1];
            string sql = string.Join(" ", args.Skip(2));
            return new QueryCommand(_store, _providerFactory, name, sql);
        }

        private TestCommand CreateInteractiveTestCommand()
        {
            string name = PromptForConnection();
            return new TestCommand(_store, _providerFactory, name);
        }

        private SchemaCommand CreateInteractiveSchemaCommand()
        {
            string name = PromptForConnection();
            return new SchemaCommand(_store, _providerFactory, name);
        }

        private string PromptForConnection()
        {
            List<Models.SavedConnection> connections = _store.GetAll();

            if (connections.Count == 0)
            {
                throw new ArgumentException("No connections saved. Use 'db add <name> <connection-string>' to add one.");
            }

            Console.WriteLine("Select a connection:");
            for (int i = 0; i < connections.Count; i++)
            {
                Models.SavedConnection connection = connections[i];
                Console.WriteLine($"  [{i + 1}] {connection.Name} ({connection.ProviderType} @ {connection.Metadata.Host}/{connection.Metadata.Database})");
            }

            Console.Write("Enter number: ");
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int index) && index >= 1 && index <= connections.Count)
            {
                return connections[index - 1].Name;
            }

            throw new ArgumentException($"Invalid selection: '{input}'");
        }
    }
}
