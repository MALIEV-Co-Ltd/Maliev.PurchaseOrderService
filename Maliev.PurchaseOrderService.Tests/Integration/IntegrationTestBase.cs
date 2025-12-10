using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Maliev.PurchaseOrderService.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Testcontainers.PostgreSql;
using WireMock.Server;
using Xunit;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Base class for integration tests using Testcontainers for PostgreSQL,
/// WireMock for external service mocking, and dynamic RSA keys for JWT auth
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private readonly RSA _testRsa;
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    protected WireMockServer? SupplierServiceMock;
    protected WireMockServer? OrderServiceMock;
    protected WireMockServer? CurrencyServiceMock;
    protected WireMockServer? UploadServiceMock;
    protected WireMockServer? PdfServiceMock;
    
    // Change type to base class to allow assignment
    protected WebApplicationFactory<Program>? Factory;
    protected HttpClient? Client;

    public IntegrationTestBase()
    {
        // Generate ephemeral RSA key for test JWT tokens
        _testRsa = RSA.Create(2048);
    }

    public async Task InitializeAsync()
    {
        // Set environment to Testing BEFORE creating the factory
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("purchaseorder_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        // Set connection string environment variable for eager configuration in Program.cs
        Environment.SetEnvironmentVariable("ConnectionStrings__PurchaseOrderDbContext", _postgresContainer.GetConnectionString());

        // Start WireMock servers for external services
        SupplierServiceMock = WireMockServer.Start();
        OrderServiceMock = WireMockServer.Start();
        CurrencyServiceMock = WireMockServer.Start();
        UploadServiceMock = WireMockServer.Start();
        PdfServiceMock = WireMockServer.Start();

        // Create custom WebApplicationFactory with test configuration
        Factory = new TestWebApplicationFactory(
            _postgresContainer.GetConnectionString(),
            _testRsa,
            SupplierServiceMock.Urls[0],
            OrderServiceMock.Urls[0],
            CurrencyServiceMock.Urls[0],
            UploadServiceMock.Urls[0],
            PdfServiceMock.Urls[0]);

        Client = Factory.CreateClient();
        
        // Ensure database is migrated
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }

        SupplierServiceMock?.Stop();
        OrderServiceMock?.Stop();
        CurrencyServiceMock?.Stop();
        UploadServiceMock?.Stop();
        PdfServiceMock?.Stop();

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }

        _testRsa.Dispose();
    }

    /// <summary>
    /// Get a scoped database context for test setup/assertions
    /// </summary>
    protected PurchaseOrderContext GetDbContext()
    {
        if (Factory == null)
            throw new InvalidOperationException("Factory not initialized");

        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
    }

    /// <summary>
    /// Creates a test JWT token with specified claims for integration testing.
    /// Uses dynamic RSA keys that match the PostConfigureAll configuration.
    /// </summary>
    /// <param name="userId">User ID claim</param>
    /// <param name="role">User role</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token string</returns>
    protected string GenerateTestToken(string userId = "test-user", string role = "employee", Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role)
        };

        // Add additional claims
        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims)
            {
                claims.Add(new Claim(key, value));
            }
        }

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_testRsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates HTTP client with JWT Bearer token authentication
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string userId = "test-user", string role = "employee", Dictionary<string, string>? additionalClaims = null)
    {
        var client = Factory!.CreateClient();
        var token = GenerateTestToken(userId, role, additionalClaims);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // Helper methods for specific roles
    protected HttpClient CreateEmployeeClient(string userId = "test-employee") => CreateAuthenticatedClient(userId, "employee");
    protected HttpClient CreateManagerClient(string userId = "test-manager") => CreateAuthenticatedClient(userId, "manager");
    protected HttpClient CreateProcurementClient(string userId = "test-procurement") => CreateAuthenticatedClient(userId, "procurement");
    protected HttpClient CreateAdminClient(string userId = "test-admin") => CreateAuthenticatedClient(userId, "admin");
}