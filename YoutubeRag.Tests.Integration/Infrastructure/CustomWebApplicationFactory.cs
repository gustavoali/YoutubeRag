using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using YoutubeRag.Infrastructure.Data;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Infrastructure.Services.Mock;
using System.Data.Common;

namespace YoutubeRag.Tests.Integration.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing with in-memory database
/// </summary>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    // Use a unique database name to avoid conflicts between tests
    private static int _databaseCounter = 0;
    private readonly string _databaseName;

    public CustomWebApplicationFactory()
    {
        // Create a unique database name for each factory instance
        _databaseName = $"InMemoryDb_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid()}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to use appsettings.Testing.json
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove the existing DbContext registration
            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            // Replace background job service with mock since Hangfire is disabled
            var backgroundJobServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBackgroundJobService));

            if (backgroundJobServiceDescriptor != null)
            {
                services.Remove(backgroundJobServiceDescriptor);
            }
            services.AddScoped<IBackgroundJobService, MockBackgroundJobService>();

            // Add in-memory database for testing with unique database name
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

                // Ensure the database is created
                db.Database.EnsureCreated();

                try
                {
                    // Seed the database with test data
                    SeedDatabase(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test messages. Error: {Message}", ex.Message);
                }
            }
        });
    }

    private void SeedDatabase(ApplicationDbContext context)
    {
        // Add seed data here if needed
        // This method will be called for each test
    }
}