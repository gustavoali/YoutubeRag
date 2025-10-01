using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace YoutubeRag.Application.DependencyInjection;

/// <summary>
/// Extension methods for IServiceCollection to register Application layer services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Application layer services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Register FluentValidation
        var assembly = Assembly.GetExecutingAssembly();

        // Register all validators from the Application assembly
        services.AddValidatorsFromAssembly(assembly);

        // Add FluentValidation to MVC
        services.AddFluentValidationAutoValidation(config =>
        {
            // Disable default model state validation since we're using FluentValidation
            config.DisableDataAnnotationsValidation = false; // Keep both for backward compatibility
        });

        // Add client-side adapters for validation (optional - for Razor Pages/MVC views)
        services.AddFluentValidationClientsideAdapters();

        // Register Application Services
        services.AddScoped<Interfaces.Services.IUserService, Services.UserService>();
        services.AddScoped<Interfaces.Services.IVideoService, Services.VideoService>();
        services.AddScoped<Interfaces.Services.IAuthService, Services.AuthService>();
        services.AddScoped<Interfaces.Services.ISearchService, Services.SearchService>();

        return services;
    }
}