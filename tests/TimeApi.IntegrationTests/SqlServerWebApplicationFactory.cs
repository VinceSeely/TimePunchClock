using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TimeApi.Services;

namespace TimeApi.IntegrationTests;

/// <summary>
/// WebApplicationFactory that uses a real SQL Server container via Testcontainers.
/// Use this for tests that need to verify SQL Server-specific behavior,
/// migrations, or database constraints.
/// </summary>
public class SqlServerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly MsSqlContainer _sqlContainer;

    public SqlServerWebApplicationFactory()
    {
        // Create SQL Server container
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!@#Strong")
            .Build();
    }

    /// <summary>
    /// Starts the SQL Server container. Call this in test setup.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TimeClockDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with connection to the test container
            services.AddDbContext<TimeClockDbContext>(options =>
            {
                options.UseSqlServer(_sqlContainer.GetConnectionString());
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<TimeClockDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
