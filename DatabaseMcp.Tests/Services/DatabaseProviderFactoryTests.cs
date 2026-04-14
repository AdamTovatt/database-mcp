using DatabaseMcp.Core.Interfaces;
using DatabaseMcp.Core.Providers;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Tests.Services
{
    public class DatabaseProviderFactoryTests
    {
        private readonly DatabaseProviderFactory _factory = new DatabaseProviderFactory();

        [Fact]
        public void Create_Postgres_ReturnsPostgresProvider()
        {
            IDatabaseProvider provider = _factory.Create("postgres");

            Assert.IsType<PostgresProvider>(provider);
        }

        [Fact]
        public void Create_Postgresql_ReturnsPostgresProvider()
        {
            IDatabaseProvider provider = _factory.Create("postgresql");

            Assert.IsType<PostgresProvider>(provider);
        }

        [Fact]
        public void Create_CaseInsensitive()
        {
            IDatabaseProvider provider = _factory.Create("POSTGRES");

            Assert.IsType<PostgresProvider>(provider);
        }

        [Fact]
        public void Create_UnknownType_ThrowsArgumentException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _factory.Create("mysql"));

            Assert.Contains("mysql", ex.Message);
            Assert.Contains("Supported", ex.Message);
        }
    }
}
