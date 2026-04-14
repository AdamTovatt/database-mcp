using DatabaseMcp.Core.Models;
using DatabaseMcp.Core.Services;

namespace DatabaseMcp.Tests.Services
{
    public class ConnectionStringParserTests
    {
        [Fact]
        public void Parse_StandardFormat_ExtractsAllFields()
        {
            string input = "Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass";

            (string normalized, ConnectionMetadata metadata, string providerType) = ConnectionStringParser.Parse(input);

            Assert.Equal("localhost", metadata.Host);
            Assert.Equal(5432, metadata.Port);
            Assert.Equal("mydb", metadata.Database);
            Assert.Equal("user", metadata.Username);
            Assert.Equal("postgres", providerType);
            Assert.Equal(input, normalized);
        }

        [Fact]
        public void Parse_PostgresUri_ExtractsAllFields()
        {
            string input = "postgres://user:pass@myhost:5433/mydb";

            (string normalized, ConnectionMetadata metadata, string providerType) = ConnectionStringParser.Parse(input);

            Assert.Equal("myhost", metadata.Host);
            Assert.Equal(5433, metadata.Port);
            Assert.Equal("mydb", metadata.Database);
            Assert.Equal("user", metadata.Username);
            Assert.Equal("postgres", providerType);
            Assert.Contains("Host=myhost", normalized);
            Assert.Contains("Port=5433", normalized);
            Assert.Contains("Database=mydb", normalized);
            Assert.Contains("Username=user", normalized);
            Assert.Contains("Password=pass", normalized);
        }

        [Fact]
        public void Parse_PostgresqlUri_ExtractsAllFields()
        {
            string input = "postgresql://admin:secret@db.example.com:5432/production";

            (_, ConnectionMetadata metadata, string providerType) = ConnectionStringParser.Parse(input);

            Assert.Equal("db.example.com", metadata.Host);
            Assert.Equal(5432, metadata.Port);
            Assert.Equal("production", metadata.Database);
            Assert.Equal("admin", metadata.Username);
            Assert.Equal("postgres", providerType);
        }

        [Fact]
        public void Parse_UriWithQueryParams_IncludesParamsInNormalized()
        {
            string input = "postgres://user:pass@host:5432/db?sslmode=require";

            (string normalized, _, _) = ConnectionStringParser.Parse(input);

            Assert.Contains("SSL Mode=require", normalized);
        }

        [Fact]
        public void Parse_UriWithEncodedPassword_DecodesCorrectly()
        {
            string input = "postgres://user:p%40ss%23word@host/db";

            (string normalized, ConnectionMetadata metadata, _) = ConnectionStringParser.Parse(input);

            Assert.Equal("user", metadata.Username);
            Assert.Contains("Password=p@ss#word", normalized);
        }

        [Fact]
        public void Parse_UriMissingPort_DefaultsTo5432()
        {
            string input = "postgres://user:pass@host/db";

            (_, ConnectionMetadata metadata, _) = ConnectionStringParser.Parse(input);

            Assert.Equal(5432, metadata.Port);
        }

        [Fact]
        public void Parse_StandardFormat_MissingPort_DefaultsTo5432()
        {
            string input = "Host=localhost;Database=mydb;Username=user;Password=pass";

            (_, ConnectionMetadata metadata, _) = ConnectionStringParser.Parse(input);

            Assert.Equal(5432, metadata.Port);
        }

        [Fact]
        public void Parse_StandardFormat_DefaultsToPostgresProvider()
        {
            string input = "Host=localhost;Database=mydb;Username=user;Password=pass";

            (_, _, string providerType) = ConnectionStringParser.Parse(input);

            Assert.Equal("postgres", providerType);
        }

        [Fact]
        public void Parse_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ConnectionStringParser.Parse(""));
        }

        [Fact]
        public void Parse_WhitespaceOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ConnectionStringParser.Parse("   "));
        }

        [Fact]
        public void Parse_StandardFormat_MissingHost_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ConnectionStringParser.Parse("Database=mydb;Username=user;Password=pass"));
        }

        [Fact]
        public void Parse_StandardFormat_MissingDatabase_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ConnectionStringParser.Parse("Host=localhost;Username=user;Password=pass"));
        }

        [Fact]
        public void Parse_UriMissingHost_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ConnectionStringParser.Parse("postgres:///mydb"));
        }

        [Fact]
        public void Parse_UriMissingDatabase_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ConnectionStringParser.Parse("postgres://user:pass@host/"));
        }

        [Fact]
        public void Parse_StandardFormat_AlternateKeyNames_Work()
        {
            string input = "Server=myserver;Initial Catalog=mydb;User Id=admin;Password=pass";

            (_, ConnectionMetadata metadata, _) = ConnectionStringParser.Parse(input);

            Assert.Equal("myserver", metadata.Host);
            Assert.Equal("mydb", metadata.Database);
            Assert.Equal("admin", metadata.Username);
        }

        [Fact]
        public void Parse_StandardFormat_CaseInsensitiveKeys()
        {
            string input = "host=localhost;PORT=5433;database=mydb;username=user;password=pass";

            (_, ConnectionMetadata metadata, _) = ConnectionStringParser.Parse(input);

            Assert.Equal("localhost", metadata.Host);
            Assert.Equal(5433, metadata.Port);
            Assert.Equal("mydb", metadata.Database);
            Assert.Equal("user", metadata.Username);
        }
    }
}
