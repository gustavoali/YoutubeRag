using System.Net;
using FluentAssertions;
using YoutubeRag.Tests.Integration.Infrastructure;
using Xunit;

namespace YoutubeRag.Tests.Integration;

/// <summary>
/// Health check tests for the API
/// </summary>
public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(CustomWebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Test that the health endpoint returns OK
    /// </summary>
    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    /// <summary>
    /// Test that the API root returns expected response
    /// </summary>
    [Fact]
    public async Task ApiRoot_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Test that Swagger UI is accessible
    /// </summary>
    [Fact]
    public async Task SwaggerUI_IsAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Test that Swagger JSON is accessible
    /// </summary>
    [Fact]
    public async Task SwaggerJson_IsAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"openapi\":");
        content.Should().Contain("\"paths\":");
    }
}