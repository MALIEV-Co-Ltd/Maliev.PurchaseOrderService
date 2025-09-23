using Microsoft.Extensions.Configuration;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Test configuration builder for standardized test configurations
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Creates a test configuration with default external service settings
    /// </summary>
    public static IConfiguration CreateTestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database - Uses PostgreSQL database for testing, supports all features
                ["ConnectionStrings:PurchaseOrderDbContext"] = Environment.GetEnvironmentVariable("ConnectionStrings__PurchaseOrderDbContext")
                    ?? "Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=postgres;",

                // External Services using structured configuration
                ["ExternalServices:SupplierService:BaseUrl"] = "https://test.api.maliev.com/suppliers",
                ["ExternalServices:SupplierService:TimeoutInSeconds"] = "30",

                ["ExternalServices:OrderService:BaseUrl"] = "https://test.api.maliev.com/orders",
                ["ExternalServices:OrderService:TimeoutInSeconds"] = "30",

                ["ExternalServices:CurrencyService:BaseUrl"] = "https://test.api.maliev.com/currencies",
                ["ExternalServices:CurrencyService:TimeoutInSeconds"] = "15",

                ["ExternalServices:UploadService:BaseUrl"] = "https://test.api.maliev.com/uploads",
                ["ExternalServices:UploadService:TimeoutInSeconds"] = "60",

                ["ExternalServices:PdfService:BaseUrl"] = "https://test.api.maliev.com/pdf",
                ["ExternalServices:PdfService:TimeoutInSeconds"] = "45",

                ["ExternalServices:AuthService:BaseUrl"] = "https://test.auth.maliev.com",
                ["ExternalServices:AuthService:TimeoutInSeconds"] = "10",

                // JWT Configuration - TEST ONLY, NOT FOR PRODUCTION
                ["JWT:SigningKey"] = "test-secret-key-for-jwt-tokens-minimum-256-bits-NOT-FOR-PRODUCTION",
                ["JWT:Issuer"] = "https://test.auth.maliev.com",
                ["JWT:Audience"] = "maliev-test-microservices",
                ["JWT:ExpirationInMinutes"] = "60",

                // CORS Configuration
                ["CORS:AllowedOrigins"] = "https://test.intranet.maliev.com,https://test.www.maliev.com",

                // Logging
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",

                // Application Settings
                ["Application:ServiceName"] = "PurchaseOrderService",
                ["Application:Version"] = "1.0.0",
                ["Application:Environment"] = "Testing",

                // Performance Settings
                ["Performance:CacheExpirationMinutes"] = "5",
                ["Performance:MaxPageSize"] = "100",
                ["Performance:DefaultPageSize"] = "20",

                // File Upload Settings
                ["FileUpload:MaxFileSizeBytes"] = "10485760", // 10MB
                ["FileUpload:AllowedExtensions"] = ".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg",

                // WHT Settings
                ["WHT:DefaultRate"] = "3.0",
                ["WHT:MaxRate"] = "10.0",
                ["WHT:CompanyTaxId"] = "0123456789012",

                // Domain Events
                ["DomainEvents:ProcessingBatchSize"] = "50",
                ["DomainEvents:RetentionDays"] = "90",
                ["DomainEvents:MaxRetryAttempts"] = "3",
            })
            .Build();

        return configuration;
    }

    /// <summary>
    /// Creates a configuration for testing external service failures
    /// </summary>
    public static IConfiguration CreateFailureTestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use invalid URLs to simulate service failures
                ["ExternalServices:SupplierService:BaseUrl"] = "https://invalid.maliev.com/suppliers",
                ["ExternalServices:OrderService:BaseUrl"] = "https://invalid.maliev.com/orders",
                ["ExternalServices:CurrencyService:BaseUrl"] = "https://invalid.maliev.com/currencies",
                ["ExternalServices:UploadService:BaseUrl"] = "https://invalid.maliev.com/uploads",
                ["ExternalServices:PdfService:BaseUrl"] = "https://invalid.maliev.com/pdf",

                // Short timeouts for testing
                ["ExternalServices:SupplierService:TimeoutInSeconds"] = "1",
                ["ExternalServices:OrderService:TimeoutInSeconds"] = "1",
                ["ExternalServices:CurrencyService:TimeoutInSeconds"] = "1",
                ["ExternalServices:UploadService:TimeoutInSeconds"] = "1",
                ["ExternalServices:PdfService:TimeoutInSeconds"] = "1",
            })
            .Build();

        return configuration;
    }

    /// <summary>
    /// Creates a configuration for development testing
    /// </summary>
    public static IConfiguration CreateDevelopmentTestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Development environment URLs
                ["ExternalServices:SupplierService:BaseUrl"] = "https://dev.api.maliev.com/suppliers",
                ["ExternalServices:OrderService:BaseUrl"] = "https://dev.api.maliev.com/orders",
                ["ExternalServices:CurrencyService:BaseUrl"] = "https://dev.api.maliev.com/currencies",
                ["ExternalServices:UploadService:BaseUrl"] = "https://dev.api.maliev.com/uploads",
                ["ExternalServices:PdfService:BaseUrl"] = "https://dev.api.maliev.com/pdf",

                // CORS for development
                ["CORS:AllowedOrigins"] = "https://dev.intranet.maliev.com,https://dev.www.maliev.com,http://localhost:3000,http://localhost:5173",

                // Development logging
                ["Logging:LogLevel:Default"] = "Debug",
                ["Logging:LogLevel:Maliev.PurchaseOrderService"] = "Debug",
            })
            .Build();

        return configuration;
    }

    /// <summary>
    /// Creates a configuration for performance testing
    /// </summary>
    public static IConfiguration CreatePerformanceTestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Longer timeouts for performance testing
                ["ExternalServices:SupplierService:TimeoutInSeconds"] = "120",
                ["ExternalServices:OrderService:TimeoutInSeconds"] = "120",
                ["ExternalServices:CurrencyService:TimeoutInSeconds"] = "60",
                ["ExternalServices:UploadService:TimeoutInSeconds"] = "300",
                ["ExternalServices:PdfService:TimeoutInSeconds"] = "180",

                // Performance settings
                ["Performance:CacheExpirationMinutes"] = "30",
                ["Performance:MaxPageSize"] = "1000",
                ["Performance:DefaultPageSize"] = "100",

                // Larger batch sizes for performance
                ["DomainEvents:ProcessingBatchSize"] = "200",

                // Reduced logging for performance
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Error",
            })
            .Build();

        return configuration;
    }
}