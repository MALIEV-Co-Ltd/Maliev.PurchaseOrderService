using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Base class for integration tests with common setup and utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory<Program>>
{
    protected readonly TestWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly Mock<ISupplierServiceClient> MockSupplierService;
    protected readonly Mock<IOrderServiceClient> MockOrderService;
    protected readonly Mock<ICurrencyServiceClient> MockCurrencyService;
    protected readonly Mock<IDomainEventService> MockDomainEventService;
    protected readonly Mock<IUploadServiceClient> MockUploadService;
    protected readonly Mock<IPdfGenerationService> MockPdfService;
    protected readonly Mock<IWHTCalculationService> MockWHTService;
    protected readonly JsonSerializerOptions JsonOptions;

    protected IntegrationTestBase(TestWebApplicationFactory<Program> factory)
    {
        MockSupplierService = new Mock<ISupplierServiceClient>();
        MockOrderService = new Mock<IOrderServiceClient>();
        MockCurrencyService = new Mock<ICurrencyServiceClient>();
        MockDomainEventService = new Mock<IDomainEventService>();
        MockUploadService = new Mock<IUploadServiceClient>();
        MockPdfService = new Mock<IPdfGenerationService>();
        MockWHTService = new Mock<IWHTCalculationService>();

        // Create a custom factory that sets up mocks
        Factory = new TestWebApplicationFactory<Program>($"TestDatabase_{GetType().Name}_{Guid.NewGuid()}")
        {
            ConfigureTestServices = services =>
            {
                // Replace external service clients with mocks
                TestWebApplicationFactory<Program>.ReplaceService(services, MockSupplierService.Object);
                TestWebApplicationFactory<Program>.ReplaceService(services, MockOrderService.Object);
                TestWebApplicationFactory<Program>.ReplaceService(services, MockCurrencyService.Object);
                TestWebApplicationFactory<Program>.ReplaceService(services, MockDomainEventService.Object);
                TestWebApplicationFactory<Program>.ReplaceService(services, MockUploadService.Object);
                TestWebApplicationFactory<Program>.ReplaceService(services, MockPdfService.Object);
                TestWebApplicationFactory<Program>.ReplaceService(services, MockWHTService.Object);

                // Configure additional test services
                ConfigureAdditionalTestServices(services);
            }
        };

        Client = Factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        SetupCommonMocks();
    }

    /// <summary>
    /// Override this method to configure additional test services
    /// </summary>
    protected virtual void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Setup common mock behaviors that most tests need
    /// </summary>
    protected virtual void SetupCommonMocks()
    {
        // Default supplier validation
        MockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateSupplierDto());

        // Default currency validation
        MockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateCurrencyDto("THB", "Thai Baht"));

        // Default order items
        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    Id = 1,
                    Quantity = 1,
                    UnitPrice = 1000.00m,
                    TotalPrice = 1000.00m
                }
            });

        // Default domain event publishing
        MockDomainEventService
            .Setup(x => x.PublishEventAsync(It.IsAny<DomainEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Default WHT calculation
        MockWHTService
            .Setup(x => x.CalculateWHTAsync(It.IsAny<SupplierDto>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WHTCalculationResult
            {
                WHTAmount = 30.00m,
                NetAmount = 970.00m,
                WHTRate = 0.03m,
                IsApplicable = true
            });
    }

    #region Authentication Helpers

    protected void SetupEmployeeAuthentication(string userId = "emp123", string department = "department1")
    {
        var token = TestJwtHelper.GenerateEmployeeToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupManagerAuthentication(string userId = "mgr123", string department = "department1")
    {
        var token = TestJwtHelper.GenerateManagerToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupProcurementAuthentication(string userId = "proc123", string department = "procurement")
    {
        var token = TestJwtHelper.GenerateProcurementToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupAdminAuthentication(string userId = "admin123", string department = "admin")
    {
        var token = TestJwtHelper.GenerateAdminToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupExpiredAuthentication(string userId = "expired123", string role = "Employee")
    {
        var token = TestJwtHelper.GenerateExpiredToken(userId, role);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region HTTP Helpers

    protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        return await Client.PostAsync(requestUri, content);
    }

    protected async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        return await Client.PutAsync(requestUri, content);
    }

    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    #endregion

    #region Database Helpers

    protected Task<PurchaseOrderContext> GetDbContextAsync()
    {
        var scope = Factory.Services.CreateScope();
        return Task.FromResult(scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>());
    }

    protected async Task ExecuteInDbContextAsync(Func<PurchaseOrderContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        await action(dbContext);
    }

    protected async Task<T> ExecuteInDbContextAsync<T>(Func<PurchaseOrderContext, Task<T>> func)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        return await func(dbContext);
    }

    #endregion

    #region Test Data Creation Helpers

    protected CreatePurchaseOrderRequest CreateBasicPurchaseOrderRequest(
        OrderType orderType = OrderType.Internal,
        string currencyCode = "THB")
    {
        return new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = orderType,
            CustomerPO = "CUST-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14),
            Notes = "Test purchase order",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = Data.Enums.AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "123 Test Street",
                City = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test@maliev.com"
            }
        };
    }

    protected CreateAddressRequest CreateBasicAddressRequest(Data.Enums.AddressType addressType = Data.Enums.AddressType.Shipping)
    {
        return new CreateAddressRequest
        {
            AddressType = addressType,
            ContactName = "Test Contact",
            AddressLine1 = "123 Test Street",
            City = "Bangkok",
            PostalCode = "10100",
            Country = "Thailand",
            PhoneNumber = "+66-2-555-0123",
            EmailAddress = "test@maliev.com"
        };
    }

    #endregion

}