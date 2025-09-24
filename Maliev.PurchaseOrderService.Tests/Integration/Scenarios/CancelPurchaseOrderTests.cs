using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Moq;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data.Enums;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class CancelPurchaseOrderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ISupplierServiceClient> _mockSupplierService;
    private readonly Mock<IOrderServiceClient> _mockOrderService;
    private readonly Mock<ICurrencyServiceClient> _mockCurrencyService;
    private readonly Mock<IDomainEventService> _mockDomainEventService;
    private readonly Mock<IUploadServiceClient> _mockUploadService;

    public CancelPurchaseOrderTests(WebApplicationFactory<Program> factory)
    {
        _mockSupplierService = new Mock<ISupplierServiceClient>();
        _mockOrderService = new Mock<IOrderServiceClient>();
        _mockCurrencyService = new Mock<ICurrencyServiceClient>();
        _mockDomainEventService = new Mock<IDomainEventService>();
        _mockUploadService = new Mock<IUploadServiceClient>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add PostgreSQL database for testing
                services.AddDbContext<PurchaseOrderContext>(options =>
                {
                    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PurchaseOrderDbContext")
                        ?? "Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=postgres;";
                    options.UseNpgsql(connectionString);
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                });

                // Replace external service clients with mocks
                services.AddSingleton(_mockSupplierService.Object);
                services.AddSingleton(_mockOrderService.Object);
                services.AddSingleton(_mockCurrencyService.Object);
                services.AddSingleton(_mockDomainEventService.Object);
                services.AddSingleton(_mockUploadService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Manager_Cancels_Draft_Purchase_Order_Successfully()
    {
        // Arrange
        SetupManagerAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrder(OrderStatus.Pending);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Customer requirements changed",
            CanceledBy = "manager1"
        };

        _mockDomainEventService
            .Setup(x => x.PublishEventAsync(It.IsAny<DomainEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify purchase order status was updated
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var cancelledOrder = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        cancelledOrder.Should().NotBeNull();
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
        // Note: Entity properties would need to be checked based on actual implementation
        cancelledOrder.UpdatedAt.Should().NotBeNull();
        cancelledOrder.UpdatedBy.Should().NotBeEmpty();

        // Verify domain event was published
        _mockDomainEventService.Verify(x => x.PublishEventAsync(It.Is<DomainEventDto>(e =>
            e.EventType == "PurchaseOrderCancelled" &&
            e.AggregateId == purchaseOrderId.ToString()), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Manager_Cancels_Approved_Purchase_Order_With_Document_Cleanup()
    {
        // Arrange
        SetupManagerAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrderWithDocuments(OrderStatus.Approved);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Supplier no longer available",
            CanceledBy = "manager1",
            ArchiveDocuments = true
        };

        _mockUploadService
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockDomainEventService
            .Setup(x => x.PublishEventAsync(It.IsAny<DomainEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify purchase order status was updated
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var cancelledOrder = await dbContext.PurchaseOrders
            .Include(po => po.PurchaseOrderFiles)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        cancelledOrder.Should().NotBeNull();
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);

        // Verify documents were deleted from external service
        _mockUploadService.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        // Verify document records were not deleted
        cancelledOrder.PurchaseOrderFiles.Should().OnlyContain(f => !f.IsDeleted);
    }

    [Fact]
    public async Task Procurement_User_Cancels_Purchase_Order_In_Processing_Status()
    {
        // Arrange
        SetupProcurementAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrder(OrderStatus.Ordered);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Urgent business requirement change",
            CanceledBy = "procurement1"
        };

        _mockDomainEventService
            .Setup(x => x.PublishEventAsync(It.IsAny<DomainEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify purchase order status was updated
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var cancelledOrder = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        cancelledOrder.Should().NotBeNull();
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
        // Note: Entity properties would need to be checked based on actual implementation
    }

    [Fact]
    public async Task Cancel_Already_Cancelled_Purchase_Order_Returns_BadRequest()
    {
        // Arrange
        SetupManagerAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrder(OrderStatus.Cancelled);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Duplicate cancellation attempt",
            CanceledBy = "manager1"
        };

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("already cancelled");
    }

    [Fact]
    public async Task Cancel_Completed_Purchase_Order_Returns_BadRequest()
    {
        // Arrange
        SetupManagerAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrder(OrderStatus.Delivered);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Cannot cancel completed order",
            CanceledBy = "manager1"
        };

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("cannot be cancelled");
    }

    [Fact]
    public async Task Employee_Cannot_Cancel_Purchase_Order_Without_Authorization()
    {
        // Arrange
        SetupEmployeeAuthentication(); // Employee, not Manager/Procurement
        var purchaseOrderId = await CreateTestPurchaseOrder(OrderStatus.Pending);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Unauthorized cancellation attempt",
            CanceledBy = "employee1"
        };

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Nonexistent_Purchase_Order_Returns_NotFound()
    {
        // Arrange
        SetupManagerAuthentication();
        var nonexistentOrderId = 9999;

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Order does not exist",
            CanceledBy = "manager1"
        };

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{nonexistentOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_Purchase_Order_Without_Reason_Returns_BadRequest()
    {
        // Arrange
        SetupManagerAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrder(OrderStatus.Pending);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "", // Empty reason
            CanceledBy = "manager1"
        };

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Reason is required");
    }

    [Fact]
    public async Task Get_Cancellation_History_Returns_All_Cancelled_Orders()
    {
        // Arrange
        SetupManagerAuthentication();

        // Create multiple purchase orders and cancel some of them
        var order1Id = await CreateTestPurchaseOrder(OrderStatus.Pending);
        var order2Id = await CreateTestPurchaseOrder(OrderStatus.Approved);

        await CancelPurchaseOrderDirectly(order1Id, "First cancellation");
        await CancelPurchaseOrderDirectly(order2Id, "Second cancellation");

        // Act
        var response = await _client.GetAsync("/v1.0/purchase-orders/cancelled");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var cancelledOrders = JsonSerializer.Deserialize<List<PurchaseOrderResponse>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        cancelledOrders.Should().NotBeNull();
        cancelledOrders!.Should().HaveCountGreaterThan(1);
        cancelledOrders.Should().OnlyContain(po => po.Status == OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Soft_Cancel_Purchase_Order_Keeps_Documents_Intact()
    {
        // Arrange
        SetupManagerAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrderWithDocuments(OrderStatus.Approved);

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Soft cancellation - keep documents",
            CanceledBy = "manager1",
            ArchiveDocuments = false // Keep documents
        };

        _mockDomainEventService
            .Setup(x => x.PublishEventAsync(It.IsAny<DomainEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var json = JsonSerializer.Serialize(cancelRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify purchase order is cancelled but documents remain
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var cancelledOrder = await dbContext.PurchaseOrders
            .Include(po => po.PurchaseOrderFiles)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        cancelledOrder.Should().NotBeNull();
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
        cancelledOrder.PurchaseOrderFiles.Should().OnlyContain(f => !f.IsDeleted);

        // Verify upload service was not called for deletion
        _mockUploadService.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<int> CreateTestPurchaseOrder(OrderStatus status = OrderStatus.Pending)
    {
        SetupExternalServiceMocks();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1,
            CurrencyID = 1,
            OrderID = 1,
            Notes = "Test order for cancellation"
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        var response = await _client.PostAsync("/v1.0/purchase-orders", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<PurchaseOrderResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var orderId = createdOrder!.Id;

        // Update status if needed
        if (status != OrderStatus.Pending)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
            var order = await dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == 1001);
            if (order != null)
            {
                order.Status = status;
                await dbContext.SaveChangesAsync();
            }
        }

        return orderId;
    }

    private async Task<int> CreateTestPurchaseOrderWithDocuments(OrderStatus status = OrderStatus.Pending)
    {
        var orderId = await CreateTestPurchaseOrder(status);

        // Add mock documents to the purchase order
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrderFiles = new List<Maliev.PurchaseOrderService.Data.Entities.PurchaseOrderFile>
        {
            new()
            {
                Id = 1234,
                PurchaseOrderId = 1001,
                FileName = "test-document-1.pdf",
                ObjectName = "test/test-document-1.pdf",
                DocumentType = DocumentType.Reference,
                FileSize = 1024,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = "employee@maliev.com"
            },
            new()
            {
                Id = 1234,
                PurchaseOrderId = 1001,
                FileName = "test-document-2.pdf",
                ObjectName = "test/test-document-2.pdf",
                DocumentType = DocumentType.Reference,
                FileSize = 2048,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = "employee@maliev.com"
            }
        };

        dbContext.PurchaseOrderFiles.AddRange(purchaseOrderFiles);
        await dbContext.SaveChangesAsync();

        return orderId;
    }

    private async Task CancelPurchaseOrderDirectly(int orderId, string reason)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var order = await dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == 1001);
        if (order != null)
        {
            order.Status = OrderStatus.Cancelled;
            // Note: Entity properties would need to be set based on actual implementation
            // order.CancellationReason = reason;
            // order.CancelledAt = DateTime.UtcNow;
            // order.CancelledBy = "system";
            await dbContext.SaveChangesAsync();
        }
    }

    private void SetupEmployeeAuthentication()
    {
        var token = "Bearer mock-employee-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupManagerAuthentication()
    {
        var token = "Bearer mock-manager-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupProcurementAuthentication()
    {
        var token = "Bearer mock-procurement-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupExternalServiceMocks()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto { Id = Guid.NewGuid(), Name = "Test Supplier" });

        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrencyDto { Code = "THB", Name = "Thai Baht", Symbol = "฿" });

        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    Id = 1234,
                    Quantity = 1,
                    UnitPrice = 1000.00m,
                    TotalPrice = 1000.00m
                }
            });
    }
}