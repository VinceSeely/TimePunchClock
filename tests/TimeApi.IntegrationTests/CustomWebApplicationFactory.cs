using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeApi.Services;

namespace TimeApi.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Replaces SQL Server with in-memory database for fast, isolated tests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
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

            // Add in-memory database for testing with a dedicated service provider
            services.AddDbContext<TimeClockDbContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase("TestDb")
                       .UseInternalServiceProvider(null); // Force EF to create its own service provider
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
