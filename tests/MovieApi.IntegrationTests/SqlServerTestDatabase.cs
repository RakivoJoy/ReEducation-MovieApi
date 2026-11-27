using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;

namespace MovieApi.IntegrationTests;

internal sealed class SqlServerTestDatabase : IAsyncDisposable
{
    public SqlServerTestDatabase()
    {
        var serverConnection = Environment.GetEnvironmentVariable("MOVIEAPI_TEST_SQLSERVER");
        if (serverConnection is null && OperatingSystem.IsWindows())
        {
            serverConnection = "Server=lpc:localhost;Integrated Security=True;TrustServerCertificate=True";
        }

        ConnectionString = new SqlConnectionStringBuilder(serverConnection
            ?? throw new InvalidOperationException("Set MOVIEAPI_TEST_SQLSERVER to a SQL Server connection with database creation permission."))
        {
            InitialCatalog = $"MovieApiIntegrationTests_{Guid.NewGuid():N}"
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    public MovieDbContext CreateContext() => new(
        new DbContextOptionsBuilder<MovieDbContext>().UseSqlServer(ConnectionString).Options);

    public async ValueTask DisposeAsync()
    {
        await using var database = CreateContext();
        await database.Database.EnsureDeletedAsync();
    }
}
