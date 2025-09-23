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
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class EmployeeCreatesPurchaseOrderTests : IntegrationTestBase
{
    public EmployeeCreatesPurchaseOrderTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Employee_Creates_Internal_Purchase_Order_Successfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123", "department1");

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

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await DeserializeResponseAsync<PurchaseOrderResponse>(response);

        createdOrder.Should().NotBeNull();
        createdOrder!.Id.Should().BeGreaterThan(0);
        createdOrder.OrderType.Should().Be(OrderType.Internal);
        createdOrder.Status.Should().Be(OrderStatus.Pending);
        createdOrder.TotalAmount.Should().Be(2000.00m); // (10 * 100) + (5 * 200)
        // OrderItems are not included in PurchaseOrderResponse - verify via separate endpoint or database

        // Verify external service calls
        MockSupplierService.Verify(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        MockCurrencyService.Verify(x => x.ValidateCurrencyAsync("THB", It.IsAny<CancellationToken>()), Times.Once);
        MockOrderService.Verify(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Verify data persistence
        using var scope = Factory.Services.CreateScope();
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
        SetupEmployeeAuthentication("emp123", "department1");
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

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await DeserializeResponseAsync<PurchaseOrderResponse>(response);

        createdOrder.Should().NotBeNull();
        createdOrder!.OrderType.Should().Be(OrderType.External);
        createdOrder.CustomerPO.Should().Be("CUST-PO-2024-001");
        createdOrder.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Employee_Creates_Purchase_Order_With_Invalid_Supplier_Fails()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123", "department1");
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

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Supplier not found");
    }

    [Fact]
    public async Task Employee_Creates_Purchase_Order_With_Invalid_Currency_Fails()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123", "department1");
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

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Invalid currency");
    }

    [Fact]
    public async Task Employee_Creates_Purchase_Order_With_Invalid_Order_Items_Fails()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123", "department1");
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

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

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

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void SetupInvalidSupplierMock()
    {
        MockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Supplier not found"));
    }

    private void SetupInvalidCurrencyMock()
    {
        MockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync("INVALID", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Invalid currency"));
    }

    private void SetupInvalidOrderItemMock()
    {
        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Order not found"));
    }
}