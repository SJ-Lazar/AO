using AO.Core.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AO.Tests.Features;

internal sealed class FeatureTestDbScope : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private FeatureTestDbScope(SqliteConnection connection, AOContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
    }

    public AOContext DbContext { get; }

    public static async Task<FeatureTestDbScope> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AOContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new AOContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new FeatureTestDbScope(connection, dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
