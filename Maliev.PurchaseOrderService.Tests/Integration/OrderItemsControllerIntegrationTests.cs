using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using Xunit;
using FluentAssertions;
using Moq;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Maliev.PurchaseOrderService.Api.ExternalServices;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for OrderItemsController
/// Tests order item retrieval, refresh functionality, and summary statistics
/// </summary>
public class OrderItemsControllerIntegrationTests : IntegrationTestBase
{
    public OrderItemsControllerIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/items Tests

    [Fact]
    public async Task GetOrderItems_WithValidPurchaseOrderId_ShouldReturnItems()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<OrderItemDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
        result.All(item => item.PurchaseOrderId == seededPurchaseOrder.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetOrderItems_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/99999/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GetOrderItems_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderItems_WithDeletedPurchaseOrder_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Soft delete the purchase order
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders.FindAsync(seededPurchaseOrder.Id);
            if (po != null)
            {
                po.IsDeleted = true;
                po.DeletedAt = DateTime.UtcNow;
                po.DeletedBy = "test-user";
                await dbContext.SaveChangesAsync();
            }
        });

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/items/{itemId} Tests

    [Fact]
    public async Task GetOrderItem_WithValidIds_ShouldReturnItem()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Get first order item
        var orderItem = await ExecuteInDbContextAsync(async dbContext =>
        {
            return await dbContext.OrderItems
                .FirstAsync(oi => oi.PurchaseOrderId == seededPurchaseOrder.Id);
        });

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/{orderItem.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Id.Should().Be(orderItem.Id);
        result.PurchaseOrderId.Should().Be(seededPurchaseOrder.Id);
    }

    [Fact]
    public async Task GetOrderItem_WithInvalidItemId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("ORDER_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task GetOrderItem_WithMismatchedPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder1 = await SeedPurchaseOrderAsync();
        var seededPurchaseOrder2 = await SeedPurchaseOrderAsync();

        // Get order item from first PO
        var orderItem = await ExecuteInDbContextAsync(async dbContext =>
        {
            return await dbContext.OrderItems
                .FirstAsync(oi => oi.PurchaseOrderId == seededPurchaseOrder1.Id);
        });

        // Act - Try to access the item with wrong purchase order ID
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder2.Id}/items/{orderItem.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /v1.0/purchase-orders/{purchaseOrderId}/items/refresh Tests

    [Fact]
    public async Task RefreshOrderItems_WithValidPurchaseOrder_ShouldRefreshSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return updated order items
        var updatedOrderItems = new List<OrderItemDto>
        {
            new OrderItemDto
            {
                Id = 1,
                ProductCode = "PROD-001-UPDATED",
                ProductName = "Updated Product 1",
                Quantity = 2,
                UnitPrice = 1500.00m,
                TotalPrice = 3000.00m,
                UnitOfMeasure = "each"
            },
            new OrderItemDto
            {
                Id = 2,
                ProductCode = "PROD-002-NEW",
                ProductName = "New Product 2",
                Quantity = 1,
                UnitPrice = 500.00m,
                TotalPrice = 500.00m,
                UnitOfMeasure = "each"
            }
        };

        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(seededPurchaseOrder.OrderID, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedOrderItems);

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemRefreshResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PurchaseOrderId.Should().Be(seededPurchaseOrder.Id);
        result.RefreshedBy.Should().Be("emp123");
        result.NewItemCount.Should().Be(2);
        result.OrderItems.Should().HaveCount(2);

        // Verify items were actually updated in database
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var refreshedItems = await dbContext.OrderItems
                .Where(oi => oi.PurchaseOrderId == seededPurchaseOrder.Id)
                .ToListAsync();

            refreshedItems.Should().HaveCount(2);
            refreshedItems.Should().Contain(item => item.ProductCode == "PROD-001-UPDATED");
            refreshedItems.Should().Contain(item => item.ProductCode == "PROD-002-NEW");
        });
    }

    [Fact]
    public async Task RefreshOrderItems_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.PutAsync("/v1.0/purchase-orders/99999/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task RefreshOrderItems_WithNoExternalOrderId_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Create purchase order without external order ID
        var purchaseOrderWithoutOrderId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = new PurchaseOrder
            {
                OrderNumber = "PO-TEST-001",
                SupplierID = 1234,
                OrderID = 0, // No external order ID
                CurrencyID = 1,
                SupplierName = "Test Supplier",
                CurrencyCode = "THB",
                Currency = "THB",
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderType = OrderType.Internal,
                SubtotalAmount = 1000m,
                TotalAmount = 1000m,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.PurchaseOrders.AddAsync(po);
            await dbContext.SaveChangesAsync();
            return po;
        });

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{purchaseOrderWithoutOrderId.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("NO_EXTERNAL_ORDER");
    }

    [Fact]
    public async Task RefreshOrderItems_WithExternalServiceFailure_ShouldReturnSuccessWithError()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to throw exception
        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(seededPurchaseOrder.OrderID, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("External service unavailable"));

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemRefreshResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to connect to external order service");
    }

    [Fact]
    public async Task RefreshOrderItems_WithEmployeeAuth_ShouldSucceed()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RefreshOrderItems_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshOrderItems_ShouldUpdateSubtotalWhenItemsChange()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return items with different total
        var updatedOrderItems = new List<OrderItemDto>
        {
            new OrderItemDto
            {
                Id = 1,
                ProductCode = "PROD-001",
                ProductName = "Product 1",
                Quantity = 5,
                UnitPrice = 1000.00m,
                TotalPrice = 5000.00m,
                UnitOfMeasure = "each"
            }
        };

        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(seededPurchaseOrder.OrderID, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedOrderItems);

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemRefreshResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.SubtotalUpdated.Should().BeTrue();
        result.NewSubtotal.Should().Be(5000.00m);

        // Verify purchase order subtotal was updated in database
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var updatedPO = await dbContext.PurchaseOrders.FindAsync(seededPurchaseOrder.Id);
            updatedPO.Should().NotBeNull();
            updatedPO!.SubtotalAmount.Should().Be(5000.00m);
        });
    }

    #endregion

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/items/summary Tests

    [Fact]
    public async Task GetOrderItemsSummary_WithValidPurchaseOrderId_ShouldReturnSummary()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemsSummaryDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.PurchaseOrderId.Should().Be(seededPurchaseOrder.Id);
        result.TotalItems.Should().BeGreaterThan(0);
        result.TotalQuantity.Should().BeGreaterThan(0);
        result.TotalValue.Should().BeGreaterThan(0);
        result.LastUpdated.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task GetOrderItemsSummary_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/99999/items/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GetOrderItemsSummary_WithEmptyOrderItems_ShouldReturnZeroSummary()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Create purchase order without order items
        var emptyPurchaseOrder = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = new PurchaseOrder
            {
                OrderNumber = "PO-EMPTY-001",
                SupplierID = 1234,
                OrderID = 5678,
                CurrencyID = 1,
                SupplierName = "Test Supplier",
                CurrencyCode = "THB",
                Currency = "THB",
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderType = OrderType.Internal,
                SubtotalAmount = 0m,
                TotalAmount = 0m,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.PurchaseOrders.AddAsync(po);
            await dbContext.SaveChangesAsync();
            return po;
        });

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{emptyPurchaseOrder.Id}/items/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemsSummaryDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.TotalItems.Should().Be(0);
        result.TotalQuantity.Should().Be(0);
        result.TotalValue.Should().Be(0);
    }

    #endregion

    #region Authorization Tests

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Procurement")]
    [InlineData("Admin")]
    public async Task RefreshOrderItems_WithValidRoles_ShouldSucceed(string role)
    {
        // Arrange
        switch (role)
        {
            case "Employee":
                SetupEmployeeAuthentication();
                break;
            case "Manager":
                SetupManagerAuthentication();
                break;
            case "Procurement":
                SetupProcurementAuthentication();
                break;
            case "Admin":
                SetupAdminAuthentication();
                break;
        }

        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/v1.0/purchase-orders/1/items")]
    [InlineData("/v1.0/purchase-orders/1/items/1")]
    [InlineData("/v1.0/purchase-orders/1/items/summary")]
    public async Task GetEndpoints_WithoutAuthentication_ShouldReturnUnauthorized(string endpoint)
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetOrderItems_WithNegativePurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/-1/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItem_WithNegativeItemId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task RefreshOrderItems_WithLargeDataSet_ShouldCompleteWithinTimeout()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return large dataset
        var largeOrderItems = Enumerable.Range(1, 100)
            .Select(i => new OrderItemDto
            {
                Id = i,
                ProductCode = $"PROD-{i:D3}",
                ProductName = $"Product {i}",
                Quantity = 1,
                UnitPrice = 100.00m,
                TotalPrice = 100.00m,
                UnitOfMeasure = "each"
            }).ToList();

        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(seededPurchaseOrder.OrderID, It.IsAny<CancellationToken>()))
            .ReturnsAsync(largeOrderItems);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act & Assert
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Data Consistency Tests

    [Fact]
    public async Task RefreshOrderItems_ShouldMaintainDataConsistency()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var originalItemCount = await ExecuteInDbContextAsync(async dbContext =>
        {
            return await dbContext.OrderItems
                .CountAsync(oi => oi.PurchaseOrderId == seededPurchaseOrder.Id);
        });

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderItemRefreshResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.PreviousItemCount.Should().Be(originalItemCount);

        // Verify database consistency
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var newItemCount = await dbContext.OrderItems
                .CountAsync(oi => oi.PurchaseOrderId == seededPurchaseOrder.Id);

            newItemCount.Should().Be(result.NewItemCount);
        });
    }

    #endregion
}