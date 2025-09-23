using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
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

    #region External Service Mocking Helpers

    protected virtual void SetupExternalServiceMocks()
    {
        // This method can be overridden by derived classes for specific mock setups
        // The default mocks are already set up in the constructor
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

    #region Database Seeding Helpers

    /// <summary>
    /// Seeds the database with test data for scenarios that need existing purchase orders
    /// </summary>
    protected async Task SeedTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        // Ensure database is created and clean
        await dbContext.Database.EnsureCreatedAsync();

        // Use a single transaction for better performance
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // Don't seed if data already exists
            if (await dbContext.PurchaseOrders.AnyAsync())
            {
                await transaction.CommitAsync();
                return;
            }

            // Create test data
            var (purchaseOrder1, orderItems1, shippingAddress1, billingAddress1) =
                TestDataFactory.CreateCompletePurchaseOrderWithEntities(OrderType.Internal, 2, "emp123");

            var (purchaseOrder2, orderItems2, shippingAddress2, billingAddress2) =
                TestDataFactory.CreateCompletePurchaseOrderWithEntities(OrderType.External, 1, "emp456");

            var (purchaseOrder3, orderItems3, shippingAddress3, billingAddress3) =
                TestDataFactory.CreateCompletePurchaseOrderWithEntities(OrderType.Internal, 3, "emp789");

            // Set specific statuses for testing
            purchaseOrder2.Status = OrderStatus.Approved;
            purchaseOrder2.ApprovedBy = "mgr123";
            purchaseOrder2.ApprovedAt = DateTime.UtcNow.AddDays(-1);

            purchaseOrder3.Status = OrderStatus.Cancelled;
            purchaseOrder3.CancelledBy = "mgr123";
            purchaseOrder3.CancelledAt = DateTime.UtcNow.AddHours(-2);

            // Add all data in a single batch to minimize database round trips
            var allAddresses = new List<Address>();
            if (shippingAddress1 != null) allAddresses.Add(shippingAddress1);
            if (billingAddress1 != null) allAddresses.Add(billingAddress1);
            if (shippingAddress2 != null) allAddresses.Add(shippingAddress2);
            if (billingAddress2 != null) allAddresses.Add(billingAddress2);
            if (shippingAddress3 != null) allAddresses.Add(shippingAddress3);
            if (billingAddress3 != null) allAddresses.Add(billingAddress3);

            if (allAddresses.Count > 0)
            {
                await dbContext.Addresses.AddRangeAsync(allAddresses);
                await dbContext.SaveChangesAsync();
            }

            // Set address foreign keys
            purchaseOrder1.ShippingAddressId = shippingAddress1?.Id;
            purchaseOrder1.BillingAddressId = billingAddress1?.Id;
            purchaseOrder2.ShippingAddressId = shippingAddress2?.Id;
            purchaseOrder2.BillingAddressId = billingAddress2?.Id;
            purchaseOrder3.ShippingAddressId = shippingAddress3?.Id;
            purchaseOrder3.BillingAddressId = billingAddress3?.Id;

            // Add purchase orders
            await dbContext.PurchaseOrders.AddRangeAsync(purchaseOrder1, purchaseOrder2, purchaseOrder3);
            await dbContext.SaveChangesAsync();

            // Set order item foreign keys and add them in batches
            foreach (var item in orderItems1)
                item.PurchaseOrderId = purchaseOrder1.Id;
            foreach (var item in orderItems2)
                item.PurchaseOrderId = purchaseOrder2.Id;
            foreach (var item in orderItems3)
                item.PurchaseOrderId = purchaseOrder3.Id;

            var allOrderItems = orderItems1.Concat(orderItems2).Concat(orderItems3).ToList();
            await dbContext.OrderItems.AddRangeAsync(allOrderItems);
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Seeds a specific purchase order for tests that need a known entity
    /// </summary>
    protected async Task<PurchaseOrder> SeedPurchaseOrderAsync(
        OrderType orderType = OrderType.Internal,
        OrderStatus status = OrderStatus.Pending,
        string createdBy = "test-user")
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        await dbContext.Database.EnsureCreatedAsync();

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var (purchaseOrder, orderItems, shippingAddress, billingAddress) =
                TestDataFactory.CreateCompletePurchaseOrderWithEntities(orderType, 2, createdBy);

            purchaseOrder.Status = status;

            // Add addresses first in a batch
            var addresses = new List<Address>();
            if (shippingAddress != null) addresses.Add(shippingAddress);
            if (billingAddress != null) addresses.Add(billingAddress);

            if (addresses.Count > 0)
            {
                await dbContext.Addresses.AddRangeAsync(addresses);
                await dbContext.SaveChangesAsync();
            }

            // Set address foreign keys
            if (shippingAddress != null)
                purchaseOrder.ShippingAddressId = shippingAddress.Id;
            if (billingAddress != null)
                purchaseOrder.BillingAddressId = billingAddress.Id;

            // Add purchase order
            await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
            await dbContext.SaveChangesAsync();

            // Set order item foreign keys and add them
            foreach (var item in orderItems)
                item.PurchaseOrderId = purchaseOrder.Id;

            await dbContext.OrderItems.AddRangeAsync(orderItems);
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
            return purchaseOrder;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Clears all test data from the database
    /// </summary>
    protected async Task ClearTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // Use IgnoreQueryFilters to get soft-deleted entities too
            var orderItems = await dbContext.OrderItems.IgnoreQueryFilters().ToListAsync();
            var purchaseOrderFiles = await dbContext.PurchaseOrderFiles.IgnoreQueryFilters().ToListAsync();
            var purchaseOrders = await dbContext.PurchaseOrders.IgnoreQueryFilters().ToListAsync();
            var addresses = await dbContext.Addresses.IgnoreQueryFilters().ToListAsync();
            var auditLogs = await dbContext.AuditLogs.ToListAsync();
            var domainEvents = await dbContext.DomainEvents.ToListAsync();

            // Remove in correct order to avoid foreign key constraints
            if (orderItems.Count > 0)
                dbContext.OrderItems.RemoveRange(orderItems);
            if (purchaseOrderFiles.Count > 0)
                dbContext.PurchaseOrderFiles.RemoveRange(purchaseOrderFiles);
            if (purchaseOrders.Count > 0)
                dbContext.PurchaseOrders.RemoveRange(purchaseOrders);
            if (addresses.Count > 0)
                dbContext.Addresses.RemoveRange(addresses);
            if (auditLogs.Count > 0)
                dbContext.AuditLogs.RemoveRange(auditLogs);
            if (domainEvents.Count > 0)
                dbContext.DomainEvents.RemoveRange(domainEvents);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets a seeded purchase order for testing
    /// </summary>
    protected async Task<PurchaseOrder?> GetSeededPurchaseOrderAsync(OrderStatus? status = null)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var query = dbContext.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(po => po.Status == status.Value);

        return await query.FirstOrDefaultAsync();
    }

    #endregion

}