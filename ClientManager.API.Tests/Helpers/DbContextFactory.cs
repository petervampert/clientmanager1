using ClientManager.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.API.Tests.Helpers;

/// <summary>
/// Creates isolated in-memory <see cref="AppDbContext"/> instances for unit tests.
/// Each call produces a fresh database with a unique name so tests don't share state.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
