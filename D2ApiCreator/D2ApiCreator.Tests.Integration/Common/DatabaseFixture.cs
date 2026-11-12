using Microsoft.EntityFrameworkCore;
using D2ApiCreator.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

namespace D2ApiCreator.Tests.Integration.Common;

/// <summary>
/// Base class for integration tests that require PostgreSQL database.
/// The schema is created by running the raw SQL scripts from <c>infra/sql/</c> in file order —
/// the same files used for every environment. There are no EF migrations in this project.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;

    public DatabaseFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("d2apicreator_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await ApplySqlScriptsAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private async Task ApplySqlScriptsAsync()
    {
        var sqlDirectory = FindInfraSqlDirectory();
        var scripts = Directory.GetFiles(sqlDirectory, "*.sql")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Walks up from the test output directory until it finds the repository's <c>infra/sql</c> folder.
    /// </summary>
    private static string FindInfraSqlDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "infra", "sql");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate 'infra/sql' above '{AppContext.BaseDirectory}'. The integration tests need the SQL schema scripts.");
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class is intentionally empty. It's used only to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
