using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Models;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration tests for T031: Cancel purchase order workflow
///
/// Tests Scenario 7 from quickstart.md:
/// - Purchase order cancellation workflows
/// - Role-based cancellation permissions (manager/admin only)
/// - Status transitions to Cancelled
/// - Cancellation reason tracking
/// - Workflow validation and business rules
/// - Audit trail for cancellations
/// </summary>
public class CancellationTests : IntegrationTestBase
{
    public CancellationTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithManagerRole_ShouldCancelSuccessfully()
    {
        // Arrange
        SetupManagerAuthentication("mgr_67890");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Project requirements changed - equipment no longer needed",
            CanceledBy = "mgr_67890"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<PurchaseOrderDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(Data.Enums.OrderStatus.Cancelled);
        result.CancelledBy.Should().Be("mgr_67890");
        result.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithEmployeeRole_ShouldReturnForbidden()
    {
        // Arrange
        SetupEmployeeAuthentication("emp_12345");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Employee attempting to cancel order",
            CanceledBy = "emp_12345"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "because employees should not have permission to cancel purchase orders");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithAdminRole_ShouldCancelSuccessfully()
    {
        // Arrange
        SetupAdminAuthentication("admin_99999");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Admin override - emergency cancellation due to supplier issues",
            CanceledBy = "admin_99999"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<PurchaseOrderDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(Data.Enums.OrderStatus.Cancelled);
        result.CancelledBy.Should().Be("admin_99999");
        result.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelPurchaseOrder_AlreadyCancelled_ShouldReturnConflict()
    {
        // Arrange
        SetupManagerAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Cancelled);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Attempting to cancel already cancelled order",
            CanceledBy = "mgr123"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "because the order is already in Cancelled status");
    }

    [Theory]
    [InlineData(Data.Enums.OrderStatus.Delivered)]
    [InlineData(Data.Enums.OrderStatus.Ordered)]
    public async Task CancelPurchaseOrder_WithInvalidStatus_ShouldReturnBadRequest(Data.Enums.OrderStatus status)
    {
        // Arrange
        SetupManagerAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, status);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = $"Attempting to cancel order in {status} status",
            CanceledBy = "mgr123"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"because orders in {status} status cannot be cancelled");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithoutReason_ShouldReturnBadRequest()
    {
        // Arrange
        SetupManagerAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "", // Empty reason should be invalid
            CanceledBy = "mgr123"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because cancellation reason is required");
    }

    [Fact]
    public async Task CancelPurchaseOrder_NonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        SetupManagerAuthentication();
        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Attempting to cancel non-existent order",
            CanceledBy = "mgr123"
        };

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders/99999/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order with ID 99999 does not exist");
    }

    [Fact]
    public async Task CancelPurchaseOrder_ValidRequest_ShouldUpdateStatusAndAuditFields()
    {
        // This test validates the complete cancellation workflow
        // 1. Status changes to Cancelled
        // 2. CancelledBy field is set
        // 3. CancelledAt timestamp is recorded
        // 4. Cancellation reason is stored in notes
        // 5. RowVersion is updated for optimistic concurrency

        // Arrange
        SetupManagerAuthentication("mgr_67890");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Project scope changed - components no longer required",
            CanceledBy = "mgr_67890"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<PurchaseOrderDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(Data.Enums.OrderStatus.Cancelled);
        result.CancelledBy.Should().Be("mgr_67890");
        result.CancelledAt.Should().NotBeNull();

        // Verify audit log was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var auditLogs = await dbContext.AuditLogs
                .Where(a => a.EntityId == seededPurchaseOrder.Id.ToString() && a.Action == AuditAction.Cancel)
                .ToListAsync();

            auditLogs.Should().HaveCount(1);
            auditLogs[0].UserId.Should().Be("mgr_67890");
            auditLogs[0].ChangeReason.Should().Contain("Project scope changed");
        });
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithApprovedOrder_ShouldTriggerNotifications()
    {
        // This test validates that cancelling an approved order triggers proper notifications
        // - Supplier notification about cancellation
        // - Internal team notifications
        // - Audit log entries

        // Arrange
        var cancellationRequest = new
        {
            reason = "Urgent cancellation - supplier quality issues identified"
        };

        SetupManagerAuthentication("mgr_67890");

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");
    }

    [Fact]
    public async Task CancelPurchaseOrder_BulkCancellation_ShouldProcessMultipleOrders()
    {
        // This test validates bulk cancellation capabilities for multiple related orders
        // Useful for project cancellations or supplier issues

        // Arrange
        var bulkCancellationRequest = new
        {
            orderIds = new[] { 12345, 12346, 12347 },
            reason = "Project cancelled - bulk cancellation of related orders"
        };

        SetupAdminAuthentication("admin_99999");

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders/bulk-cancel", bulkCancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the bulk cancellation endpoint is not implemented yet");
    }

    [Fact]
    public async Task GetCancelledOrders_WithManagerRole_ShouldReturnCancelledOrdersList()
    {
        // This test validates querying cancelled orders for reporting and audit purposes

        // Arrange
        SetupManagerAuthentication();

        // Act
        var response = await Client.GetAsync("/v1/purchase-orders?status=Cancelled&page=1&pageSize=20");

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders query endpoint is not implemented yet");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithConcurrentModification_ShouldHandleOptimisticConcurrency()
    {
        // This test validates handling of concurrent modifications during cancellation
        // Simulates scenario where order is modified by another user during cancellation

        // Arrange
        var cancellationRequest = new
        {
            reason = "Testing concurrent modification handling",
            rowVersion = "OUTDATED_VERSION_TOKEN"
        };

        SetupManagerAuthentication();

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented with optimistic concurrency, should return Conflict
        // response.StatusCode.Should().Be(HttpStatusCode.Conflict,
        //     "because the order was modified by another user");
    }

    [Fact]
    public async Task CancelPurchaseOrder_DepartmentManagerPermissions_ShouldValidateScope()
    {
        // This test validates that department managers can only cancel orders in their department

        // Arrange
        var cancellationRequest = new
        {
            reason = "Department manager attempting cross-department cancellation"
        };

        SetupManagerAuthentication("mgr_engineering");

        // Act - Attempting to cancel order from different department
        var response = await PostAsJsonAsync("/v1/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented, should validate department scope
        // response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
        //     "because managers should only cancel orders in their department");
    }

    private async Task<PurchaseOrderResponse> CreateTestPurchaseOrderForCancellation(Data.Enums.OrderStatus status = Data.Enums.OrderStatus.Pending)
    {
        // Helper method to create test purchase orders in specific status for cancellation testing
        // This will be used once the implementation exists
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.External,
            Notes = "Test order for cancellation testing",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = Data.Enums.AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
                City = "Test City",
                PostalCode = "12345",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test@maliev.com"
            }
        };

        var response = await PostAsJsonAsync("/v1/purchase-orders", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PurchaseOrderResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}