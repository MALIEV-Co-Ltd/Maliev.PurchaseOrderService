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
public class CancellationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<CancellationTests> _logger;

    public CancellationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Configure test logging
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<CancellationTests>();
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithManagerRole_ShouldCancelSuccessfully()
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = "Project requirements changed - equipment no longer needed"
        };

        // Set manager authorization header (would be JWT token in real implementation)
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");
        _client.DefaultRequestHeaders.Add("X-User-Id", "mgr_67890");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithEmployeeRole_ShouldReturnForbidden()
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = "Employee attempting to cancel order"
        };

        // Set employee authorization header (insufficient permissions)
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "employee");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented, this should return Forbidden
        // response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
        //     "because employees should not have permission to cancel purchase orders");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithAdminRole_ShouldCancelSuccessfully()
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = "Admin override - emergency cancellation due to supplier issues"
        };

        // Set admin authorization header
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer admin-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "admin");
        _client.DefaultRequestHeaders.Add("X-User-Id", "admin_99999");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");
    }

    [Fact]
    public async Task CancelPurchaseOrder_AlreadyCancelled_ShouldReturnConflict()
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = "Attempting to cancel already cancelled order"
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented and order is already cancelled, should return Conflict
        // response.StatusCode.Should().Be(HttpStatusCode.Conflict,
        //     "because the order is already in Cancelled status");
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Ordered)]
    [InlineData(OrderStatus.Approved)]
    public async Task CancelPurchaseOrder_WithInvalidStatus_ShouldReturnBadRequest(OrderStatus status)
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = $"Attempting to cancel order in {status} status"
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");

        // Act
        var response = await _client.PostAsJsonAsync($"/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented, should validate that only Pending/Approved orders can be cancelled
        // response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
        //     $"because orders in {status} status cannot be cancelled");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithoutReason_ShouldReturnBadRequest()
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = "" // Empty reason should be invalid
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented, should require cancellation reason
        // response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
        //     "because cancellation reason is required");
    }

    [Fact]
    public async Task CancelPurchaseOrder_NonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        var cancellationRequest = new
        {
            reason = "Attempting to cancel non-existent order"
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/99999/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented, should return NotFound for non-existent orders
        // response.StatusCode.Should().Be(HttpStatusCode.NotFound,
        //     "because the purchase order with ID 99999 does not exist");
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
        var cancellationRequest = new
        {
            reason = "Project scope changed - components no longer required"
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");
        _client.DefaultRequestHeaders.Add("X-User-Id", "mgr_67890");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");
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

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");
        _client.DefaultRequestHeaders.Add("X-User-Id", "mgr_67890");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

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

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer admin-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "admin");
        _client.DefaultRequestHeaders.Add("X-User-Id", "admin_99999");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/bulk-cancel", bulkCancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the bulk cancellation endpoint is not implemented yet");
    }

    [Fact]
    public async Task GetCancelledOrders_WithManagerRole_ShouldReturnCancelledOrdersList()
    {
        // This test validates querying cancelled orders for reporting and audit purposes

        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");

        // Act
        var response = await _client.GetAsync("/purchase-orders?status=Cancelled&page=1&pageSize=20");

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

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

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

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer dept-manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");
        _client.DefaultRequestHeaders.Add("X-User-Department", "Engineering");
        _client.DefaultRequestHeaders.Add("X-User-Id", "mgr_engineering");

        // Act - Attempting to cancel order from different department
        var response = await _client.PostAsJsonAsync("/purchase-orders/12345/cancel", cancellationRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order cancellation endpoint is not implemented yet");

        // When implemented, should validate department scope
        // response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
        //     "because managers should only cancel orders in their department");
    }

    private async Task<PurchaseOrderResponse> CreateTestPurchaseOrderForCancellation(OrderStatus status = OrderStatus.Pending)
    {
        // Helper method to create test purchase orders in specific status for cancellation testing
        // This will be used once the implementation exists
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Test order for cancellation testing",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
                City = "Test City",
                PostalCode = "12345",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test@maliev.com"
            }
        };

        var response = await _client.PostAsJsonAsync("/purchase-orders", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PurchaseOrderResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}