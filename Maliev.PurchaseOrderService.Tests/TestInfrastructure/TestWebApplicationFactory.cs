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
using Microsoft.Extensions.Options;
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

                // Ensure no PostgreSQL connection strings are used in tests
                ["ConnectionStrings:DefaultConnection"] = $"InMemory:{_databaseName}",

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

                // Disable problematic database environment variables
                ["DEV_DB_HOST"] = "",
                ["DEV_DB_PORT"] = "",
                ["DEV_DB_NAME"] = "",
                ["DEV_DB_USER"] = "",
                ["DEV_DB_PASSWORD"] = "",

                // Disable HTTPS redirect and other problematic settings
                ["Security:RequireHttps"] = "false",
                ["ASPNETCORE_URLS"] = "http://localhost:0",

                // Ensure test environment logging
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
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

            // Add InMemory database for testing with consistent configuration
            services.AddDbContext<PurchaseOrderContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();

                // Suppress problematic warnings for InMemory database
                options.ConfigureWarnings(warnings =>
                {
                    // InMemory database doesn't support transactions, but we can safely ignore this warning
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);

                    // Ignore the query filter navigation warning - this is expected behavior
                    warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);

                    // Reduce noise from relationship warnings
                    warnings.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
                });
            });

            // Ensure API versioning is properly configured for tests
            // The main application already registers API versioning, but we need to ensure it's available in tests
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            // Configure API versioning for tests - simplified to avoid dependency issues
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                    new Asp.Versioning.UrlSegmentApiVersionReader(),
                    new Asp.Versioning.QueryStringApiVersionReader("version"),
                    new Asp.Versioning.HeaderApiVersionReader("X-Version")
                );
            });

            // Skip API Explorer for tests - it's not essential for test functionality

            services.AddSwaggerGen();

            // Add AutoMapper configuration (missing from original test setup)
            services.AddAutoMapper(typeof(Program));

            // Ensure application services are registered
            // The main application services need to be available in tests

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
        const string testSigningKey = "test-signing-key-that-is-at-least-32-characters-long-for-testing-purposes";
        const string testIssuer = "test-issuer";
        const string testAudience = "test-audience";

        // Remove existing JWT Bearer authentication handlers if they exist
        var descriptorsToRemove = services.Where(d =>
            d.ServiceType == typeof(JwtBearerHandler) ||
            d.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>) ||
            d.ServiceType == typeof(IPostConfigureOptions<JwtBearerOptions>)).ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }

        // Configure JWT options to match TestJwtHelper constants
        services.Configure<JwtOptions>(options =>
        {
            options.SecurityKey = testSigningKey;
            options.Issuer = testIssuer;
            options.Audience = testAudience;
            options.ValidateIssuer = true;
            options.ValidateAudience = true;
            options.ValidateLifetime = true;
            options.ValidateIssuerSigningKey = true;
            options.ExpirationMinutes = 60;
            options.ClockSkew = "00:05:00";
        });

        // Override JWT Bearer options configuration with consistent settings
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = testIssuer,
                ValidAudience = testAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testSigningKey)),
                ClockSkew = TimeSpan.FromMinutes(5),
                // Additional settings for better test reliability
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Disable HTTPS requirement for tests
            options.RequireHttpsMetadata = false;

            // Set event handlers for debugging
            if (System.Diagnostics.Debugger.IsAttached)
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        System.Diagnostics.Debug.WriteLine($"JWT Auth Failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        System.Diagnostics.Debug.WriteLine("JWT Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            }
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