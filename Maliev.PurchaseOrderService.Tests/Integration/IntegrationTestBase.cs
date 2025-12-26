using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Maliev.PurchaseOrderService.Data;
using System.Net.Http.Headers;
using WireMock.Server;
using Xunit;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Base class for integration tests using standardized BaseIntegrationTestFactory.
/// Provides access to WireMock servers for external service mocking.
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    // Expose WireMock servers for test configuration
    protected WireMockServer SupplierServiceMock => Factory.SupplierServiceMock;
    protected WireMockServer OrderServiceMock => Factory.OrderServiceMock;
    protected WireMockServer CurrencyServiceMock => Factory.CurrencyServiceMock;
    protected WireMockServer UploadServiceMock => Factory.UploadServiceMock;
    protected WireMockServer PdfServiceMock => Factory.PdfServiceMock;
    protected WireMockServer IAMServiceMock => Factory.IAMServiceMock;

    public async Task InitializeAsync()
    {
        // BaseIntegrationTestFactory handles all container initialization
        Factory = new TestWebApplicationFactory();
        await Factory.InitializeAsync();

        // Trigger server creation
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
            // Stop WireMock servers before disposing factory
            SupplierServiceMock?.Stop();
            OrderServiceMock?.Stop();
            CurrencyServiceMock?.Stop();
            UploadServiceMock?.Stop();
            PdfServiceMock?.Stop();
            IAMServiceMock?.Stop();

            await Factory.DisposeAsync();
        }
    }

    /// <summary>
    /// Get a scoped database context for test setup/assertions
    /// </summary>
    protected PurchaseOrderContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
    }

    /// <summary>
    /// Creates HTTP client with JWT Bearer token authentication
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null, string[]? permissions = null)
    {
        return Factory.CreateAuthenticatedClient(userId, roles, permissions);
    }

    protected static readonly string[] EmployeeRoles = { "employee" };
    protected static readonly string[] ManagerRoles = { "manager" };
    protected static readonly string[] ProcurementRoles = { "procurement" };
    protected static readonly string[] AdminRoles = { "admin" };
}
