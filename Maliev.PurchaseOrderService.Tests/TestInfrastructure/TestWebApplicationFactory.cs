using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;
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

        // Configure app settings to override any problematic configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration sources that might cause issues
            config.Sources.Clear();

            // Add basic configuration
            config.AddJsonFile("appsettings.Testing.json", optional: true);

            // Add in-memory configuration to override any problematic settings
            var testConfig = new Dictionary<string, string?>
            {
                // Override connection string to prevent environment variable resolution issues
                ["ConnectionStrings:PurchaseOrderDbContext"] = $"InMemory:{_databaseName}",

                // Override external service URLs to prevent resolution issues
                ["ExternalServices:SupplierService:BaseUrl"] = "http://localhost:5001/mock/suppliers",
                ["ExternalServices:OrderService:BaseUrl"] = "http://localhost:5002/mock/orders",
                ["ExternalServices:CurrencyService:BaseUrl"] = "http://localhost:5003/mock/currencies",
                ["ExternalServices:UploadService:BaseUrl"] = "http://localhost:5004/mock/uploads",
                ["ExternalServices:PdfService:BaseUrl"] = "http://localhost:5005/mock/pdfs",
                ["ExternalServices:AuthService:BaseUrl"] = "http://localhost:5006/mock/auth",

                // Override JWT settings
                ["JWT_SIGNING_KEY"] = "test-signing-key-that-is-at-least-32-characters-long-for-testing-purposes",
                ["JWT_ISSUER"] = "test-issuer",
                ["JWT_AUDIENCE"] = "test-audience",

                // Disable HTTPS redirect and other problematic settings
                ["Security:RequireHttps"] = "false",
                ["ASPNETCORE_URLS"] = "http://localhost:0"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove all database-related service registrations more comprehensively
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>) ||
                           d.ServiceType == typeof(PurchaseOrderContext) ||
                           d.ServiceType == typeof(DbContext) ||
                           d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<PurchaseOrderContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();

                // Suppress transaction warnings for InMemory database
                // InMemory database doesn't support transactions, but we can safely ignore this warning
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Note: External service mocks will be replaced by IntegrationTestBase
            // Set up default mocks here, but they will be overridden if IntegrationTestBase provides custom ones
            var defaultSupplierMock = MockServiceFactory.CreateSupplierServiceMock().Object;
            var defaultOrderMock = MockServiceFactory.CreateOrderServiceMock().Object;
            var defaultCurrencyMock = MockServiceFactory.CreateCurrencyServiceMock().Object;
            var defaultUploadMock = MockServiceFactory.CreateUploadServiceMock().Object;
            var defaultPdfClientMock = MockServiceFactory.CreatePdfServiceClientMock().Object;

            // Replace external service clients with default mocks first
            ReplaceService(services, defaultSupplierMock);
            ReplaceService(services, defaultOrderMock);
            ReplaceService(services, defaultCurrencyMock);
            ReplaceService(services, defaultUploadMock);
            ReplaceService(services, defaultPdfClientMock);

            // Keep application services as real services for proper database integration
            // Only mock the external ones that make HTTP calls
            // Note: DomainEventService should remain real for database persistence
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

    /// <summary>
    /// Clean up resources after tests
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // SQLite in-memory databases are automatically cleaned up
            // No file cleanup needed
        }
        base.Dispose(disposing);
    }
}