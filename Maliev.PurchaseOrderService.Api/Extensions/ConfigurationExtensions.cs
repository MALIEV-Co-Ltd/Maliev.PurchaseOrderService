using Maliev.PurchaseOrderService.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Maliev.PurchaseOrderService.Api.Extensions;

/// <summary>
/// Extension methods for configuring application options
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds configuration options to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure external service options
        services.Configure<ExternalServiceOptions>(
            configuration.GetSection(ExternalServiceOptions.SectionName));

        // Configure application options
        services.Configure<ApplicationOptions>(
            configuration.GetSection(ApplicationOptions.SectionName));

        // Configure cache options
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));

        // Configure security options
        services.Configure<SecurityOptions>(
            configuration.GetSection(SecurityOptions.SectionName));

        // Configure health check options
        services.Configure<HealthCheckConfigOptions>(
            configuration.GetSection(HealthCheckConfigOptions.SectionName));

        // Add validation for required configurations
        services.AddSingleton<IValidateOptions<ExternalServiceOptions>, ExternalServiceOptionsValidator>();
        services.AddSingleton<IValidateOptions<ApplicationOptions>, ApplicationOptionsValidator>();

        return services;
    }
}

/// <summary>
/// Validator for external service options
/// </summary>
public class ExternalServiceOptionsValidator : IValidateOptions<ExternalServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, ExternalServiceOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.UploadService.BaseUrl))
            failures.Add("UploadService BaseUrl is required");

        if (string.IsNullOrWhiteSpace(options.PdfService.BaseUrl))
            failures.Add("PdfService BaseUrl is required");

        if (string.IsNullOrWhiteSpace(options.AuthService.BaseUrl))
            failures.Add("AuthService BaseUrl is required");

        if (options.UploadService.TimeoutInSeconds <= 0)
            failures.Add("UploadService TimeoutInSeconds must be greater than 0");

        if (options.PdfService.TimeoutInSeconds <= 0)
            failures.Add("PdfService TimeoutInSeconds must be greater than 0");

        if (options.AuthService.TimeoutInSeconds <= 0)
            failures.Add("AuthService TimeoutInSeconds must be greater than 0");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for application options
/// </summary>
public class ApplicationOptionsValidator : IValidateOptions<ApplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            failures.Add("ServiceName is required");

        if (string.IsNullOrWhiteSpace(options.Version))
            failures.Add("Version is required");

        if (string.IsNullOrWhiteSpace(options.Environment))
            failures.Add("Environment is required");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}