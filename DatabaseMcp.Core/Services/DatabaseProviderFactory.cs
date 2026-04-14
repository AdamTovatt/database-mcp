using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Providers;

namespace DatabaseMcp.Core.Services
{
    /// <summary>
    /// Creates database provider instances based on a provider type string.
    /// </summary>
    public class DatabaseProviderFactory : IDatabaseProviderFactory
    {
        /// <inheritdoc/>
        public IDatabaseProvider Create(string providerType)
        {
            return providerType.ToLowerInvariant() switch
            {
                "postgres" or "postgresql" => new PostgresProvider(),
                _ => throw new ArgumentException(
                    $"Unsupported database provider: '{providerType}'. Supported providers: postgres"),
            };
        }
    }
}
