using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeApi.Services;

namespace TimeApi.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Replaces SQL Server with in-memory database for fast, isolated tests.
/// Each instance uses a unique database name to ensure test isolation.
/// The in-memory database is automatically cleaned up when the factory is disposed.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.Name.Contains("DbContext") ||
                           d.ServiceType.Name.Contains("DbContextOptions"))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing with unique name for isolation
            services.AddDbContext<TimeClockDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
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
}
