using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>
/// Starts the real API in memory for tests. SQL Server is swapped for in-memory SQLite, which (unlike
/// EF's InMemory provider) enforces unique constraints and foreign keys, including the composite
/// (EventId, CategoryId) key. Tests therefore need no SQL Server, locally or in CI.
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" is not "Development", so the API skips auto-migrate, seeding and Swagger.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var sqlServerOptions = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (sqlServerOptions is not null)
            {
                services.Remove(sqlServerOptions);
            }

            _connection.Open(); // an in-memory SQLite database lives only while this connection is open
            services.AddDbContext<RaceDayDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}