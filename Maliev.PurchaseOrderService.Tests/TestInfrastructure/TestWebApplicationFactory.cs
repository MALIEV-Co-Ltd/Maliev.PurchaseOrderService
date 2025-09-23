using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Configuration;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Customized WebApplicationFactory for integration tests with proper database configuration
/// Ensures InMemory database is used and resolves database provider conflicts
/// </summary>
public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _databaseName;

    public TestWebApplicationFactory() : this(null)
    {
    }

    internal TestWebApplicationFactory(string? databaseName)
    {
        _databaseName = databaseName ?? $"TestDatabase_{typeof(TProgram).Name}_{Guid.NewGuid()}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all database-related service registrations
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PurchaseOrderContext));
            if (dbContextServiceDescriptor != null)
                services.Remove(dbContextServiceDescriptor);

            // Remove any DbContext<T> registrations
            var genericDbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContext));
            if (genericDbContextDescriptor != null)
                services.Remove(genericDbContextDescriptor);

            // Add InMemory database for testing
            services.AddDbContext<PurchaseOrderContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Replace external service clients with mocks
            ReplaceService(services, MockServiceFactory.CreateSupplierServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreateOrderServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreateCurrencyServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreateUploadServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreatePdfServiceClientMock().Object);

            // Replace application services with mocks
            ReplaceService(services, MockServiceFactory.CreateDomainEventServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreatePdfGenerationServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreateWHTCalculationServiceMock().Object);
            ReplaceService(services, MockServiceFactory.CreateDocumentManagementServiceMock().Object);

            // Configure test JWT authentication to match TestJwtHelper
            ConfigureTestAuthentication(services);

            // Allow derived classes to configure additional services
            ConfigureAdditionalTestServices(services);
        });
    }

    /// <summary>
    /// Action to configure additional test services (can be set by caller)
    /// </summary>
    public Action<IServiceCollection>? ConfigureTestServices { get; set; }

    /// <summary>
    /// Override this method in derived classes to configure additional test services
    /// </summary>
    protected virtual void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        // Override in derived classes if needed
        ConfigureTestServices?.Invoke(services);
    }

    /// <summary>
    /// Configures test JWT authentication to match TestJwtHelper configuration
    /// </summary>
    private void ConfigureTestAuthentication(IServiceCollection services)
    {
        // Remove existing JWT Bearer authentication if it exists
        var jwtDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(JwtBearerHandler));
        if (jwtDescriptor != null)
        {
            services.Remove(jwtDescriptor);
        }

        // Configure JWT options to match TestJwtHelper constants
        services.Configure<JwtOptions>(options =>
        {
            options.SecurityKey = "test-signing-key-that-is-at-least-32-characters-long-for-testing-purposes";
            options.Issuer = "test-issuer";
            options.Audience = "test-audience";
            options.ValidateIssuer = true;
            options.ValidateAudience = true;
            options.ValidateLifetime = true;
            options.ValidateIssuerSigningKey = true;
            options.ClockSkew = "00:05:00";
        });

        // Override JWT Bearer options configuration
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "test-issuer",
                ValidAudience = "test-audience",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-signing-key-that-is-at-least-32-characters-long-for-testing-purposes")),
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        });
    }

    /// <summary>
    /// Helper method to replace a service with a mock or test implementation
    /// </summary>
    public static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddSingleton(implementation);
    }
}