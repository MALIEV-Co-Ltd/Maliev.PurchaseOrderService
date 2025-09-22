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
using Maliev.PurchaseOrderService.Data.Enums;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class EmployeeCreatesPurchaseOrderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ISupplierServiceClient> _mockSupplierService;
    private readonly Mock<IOrderServiceClient> _mockOrderService;
    private readonly Mock<ICurrencyServiceClient> _mockCurrencyService;
    private readonly Mock<IUploadServiceClient> _mockUploadService;

    public EmployeeCreatesPurchaseOrderTests(WebApplicationFactory<Program> factory)
    {
        _mockSupplierService = new Mock<ISupplierServiceClient>();
        _mockOrderService = new Mock<IOrderServiceClient>();
        _mockCurrencyService = new Mock<ICurrencyServiceClient>();
        _mockUploadService = new Mock<IUploadServiceClient>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing
                services.AddDbContext<PurchaseOrderContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_EmployeeCreates");
                });

                // Replace external service clients with mocks
                services.AddSingleton(_mockSupplierService.Object);
                services.AddSingleton(_mockOrderService.Object);
                services.AddSingleton(_mockCurrencyService.Object);
                services.AddSingleton(_mockUploadService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Employee_Creates_Internal_Purchase_Order_Successfully()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupExternalServiceMocks();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,  // "THB",
            Notes = "Test internal purchase order",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 10,
                    UnitPrice = 100.00m,
                    Notes = "Test item 1"
                },
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 5,
                    UnitPrice = 200.00m,
                    Notes = "Test item 2"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/api/purchaseorders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<PurchaseOrderResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        createdOrder.Should().NotBeNull();
        createdOrder!.Id.Should().BeGreaterThan(0);
        createdOrder.OrderType.Should().Be(OrderType.Internal);
        createdOrder.Status.Should().Be(OrderStatus.Pending);
        createdOrder.TotalAmount.Should().Be(2000.00m); // (10 * 100) + (5 * 200)
        // OrderItems are not included in PurchaseOrderResponse - verify via separate endpoint or database

        // Verify external service calls
        _mockSupplierService.Verify(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCurrencyService.Verify(x => x.ValidateCurrencyAsync("THB", It.IsAny<CancellationToken>()), Times.Once);
        _mockOrderService.Verify(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Verify data persistence
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders
            .Include(po => po.OrderItems)
            .FirstOrDefaultAsync(po => po.Id == createdOrder.Id);

        savedOrder.Should().NotBeNull();
        savedOrder!.OrderItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task Employee_Creates_External_Purchase_Order_With_Customer_PO_Number()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupExternalServiceMocks();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.External,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,  // "USD",
            CustomerPO = "CUST-PO-2024-001",
            Notes = "External order with customer PO",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 1500.00m,
                    Notes = "External item"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/api/purchaseorders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<PurchaseOrderResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        createdOrder.Should().NotBeNull();
        createdOrder!.OrderType.Should().Be(OrderType.External);
        createdOrder.CustomerPO.Should().Be("CUST-PO-2024-001");
        createdOrder.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Employee_Creates_Purchase_Order_With_Invalid_Supplier_Fails()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupInvalidSupplierMock();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,  // "THB",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/api/purchaseorders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Supplier not found");
    }

    [Fact]
    public async Task Employee_Creates_Purchase_Order_With_Invalid_Currency_Fails()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupValidSupplierMock();
        SetupInvalidCurrencyMock();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,  // "INVALID",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/api/purchaseorders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Invalid currency");
    }

    [Fact]
    public async Task Employee_Creates_Purchase_Order_With_Invalid_Order_Items_Fails()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupValidSupplierMock();
        SetupValidCurrencyMock();
        SetupInvalidOrderItemMock();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,  // "THB",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/api/purchaseorders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Unauthorized_User_Cannot_Create_Purchase_Order()
    {
        // Arrange - No authentication token
        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,  // "THB",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/api/purchaseorders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void SetupEmployeeAuthentication()
    {
        var token = "Bearer mock-employee-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupExternalServiceMocks()
    {
        SetupValidSupplierMock();
        SetupValidCurrencyMock();
        SetupValidOrderItemMock();
    }

    private void SetupValidSupplierMock()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Supplier",
                Email = "test@supplier.com"
            });
    }

    private void SetupInvalidSupplierMock()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Supplier not found"));
    }

    private void SetupValidCurrencyMock()
    {
        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrencyDto
            {
                Code = "THB",
                Name = "Thai Baht",
                Symbol = "฿"
            });
    }

    private void SetupInvalidCurrencyMock()
    {
        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync("INVALID", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Invalid currency"));
    }

    private void SetupValidOrderItemMock()
    {
        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    Id = 1,
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m
                }
            });
    }

    private void SetupInvalidOrderItemMock()
    {
        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Order not found"));
    }
}