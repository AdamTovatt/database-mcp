namespace DatabaseMcp.Core.Interfaces
{
    /// <summary>
    /// Factory for creating database providers from a provider type string.
    /// </summary>
    public interface IDatabaseProviderFactory
    {
        /// <summary>
        /// Creates a database provider for the specified type.
        /// </summary>
        /// <param name="providerType">The provider type (e.g. "postgres", "sqlite").</param>
        /// <returns>The database provider instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the provider type is not supported.</exception>
        IDatabaseProvider Create(string providerType);
    }
}
