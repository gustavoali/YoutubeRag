using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.DTOs.Auth;
using YoutubeRag.Infrastructure.Data;
using Xunit;

namespace YoutubeRag.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for all integration tests
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected IServiceScope Scope = null!;
    protected ApplicationDbContext DbContext = null!;

    protected IntegrationTestBase(CustomWebApplicationFactory<Program> factory)
    {
        Factory = factory;

        // Create a new client for each test
        Client = Factory.WithWebHostBuilder(builder =>
        {
            // Additional configuration for individual test if needed
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Set default headers
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public virtual async Task InitializeAsync()
    {
        // Create a new scope for each test
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Clear database before each test
        await ClearDatabase();

        // Seed test data if needed
        await SeedTestData();
    }

    public virtual async Task DisposeAsync()
    {
        // Dispose the scope
        Scope?.Dispose();

        // Dispose the client
        Client?.Dispose();
    }

    /// <summary>
    /// Clears all data from the database
    /// </summary>
    protected virtual async Task ClearDatabase()
    {
        // Clear all entities
        DbContext.Users.RemoveRange(DbContext.Users);
        DbContext.Videos.RemoveRange(DbContext.Videos);
        DbContext.Jobs.RemoveRange(DbContext.Jobs);
        DbContext.TranscriptSegments.RemoveRange(DbContext.TranscriptSegments);

        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds test data into the database
    /// </summary>
    protected virtual async Task SeedTestData()
    {
        // Override in derived classes to add test data
        await Task.CompletedTask;
    }

    /// <summary>
    /// Authenticates and sets the authorization header for the client
    /// </summary>
    protected async Task<string> AuthenticateAsync(string email = "test@example.com", string password = "Test123!")
    {
        // First register the user
        var authService = Scope.ServiceProvider.GetRequiredService<IAuthService>();

        try
        {
            // Try to register the user
            var registerDto = new RegisterRequestDto
            {
                Email = email,
                Password = password,
                Name = "Test User"
            };

            await authService.RegisterAsync(registerDto);
        }
        catch
        {
            // User might already exist, try to login
        }

        // Login to get token
        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var tokenResponse = await authService.LoginAsync(loginDto);

        // Set the authorization header
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        return tokenResponse.AccessToken;
    }

    /// <summary>
    /// Removes the authorization header
    /// </summary>
    protected void RemoveAuthenticationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }
}