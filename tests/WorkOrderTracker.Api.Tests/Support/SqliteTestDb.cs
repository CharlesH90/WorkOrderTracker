using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Services;

namespace WorkOrderTracker.Api.Tests.Support;

/// <summary>
/// A throwaway SQLite in-memory database with the real EF model and the HasData seed
/// (2 buildings, 2 technicians, 2 work orders). Foreign keys and unique indexes are enforced.
/// </summary>
public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=True");

    public AppDbContext Db { get; }
    public RunSqlBeforeSave Interceptor { get; }

    public SqliteTestDb()
    {
        _connection.Open();
        Interceptor = new RunSqlBeforeSave(_connection);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(Interceptor)
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
    }

    public WorkOrderService CreateService(TimeProvider? clock = null) =>
        new(Db, clock ?? TimeProvider.System, Options.Create(new BusinessOptions()));

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

/// <summary>
/// Runs a raw SQL statement right before the next SaveChanges, to simulate a concurrent
/// request winning a race between a controller's check and its write.
/// </summary>
public sealed class RunSqlBeforeSave(SqliteConnection connection) : SaveChangesInterceptor
{
    /// <summary>Statement to run before the next save. Cleared after it runs once.</summary>
    public string? Sql { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Sql is not null)
        {
            var sql = Sql;
            Sql = null;
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

/// <summary>A clock frozen at a chosen instant.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
