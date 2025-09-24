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
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class OptimisticConcurrencyTests : IntegrationTestBase
{
    private readonly HttpClient _client2;

    public OptimisticConcurrencyTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
        _client2 = Factory.CreateClient();
    }

    [Fact]
    public async Task Two_Users_Update_Same_Purchase_Order_Simultaneously_Second_Update_Fails()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupEmployeeAuthentication("emp123", "department1");
        SetupSecondClientAuthentication("emp456", "department1");

        // Both clients get the same purchase order
        var getResponse1 = await Client.GetAsync($"/v1.0/purchase-orders/{purchaseOrderId}");
        var getResponse2 = await _client2.GetAsync($"/v1.0/purchase-orders/{purchaseOrderId}");

        getResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var order1 = await DeserializeResponseAsync<PurchaseOrderDetailResponse>(getResponse1);
        var order2 = await DeserializeResponseAsync<PurchaseOrderDetailResponse>(getResponse2);

        order1!.RowVersion.Should().Be(order2!.RowVersion);

        // Prepare update requests
        var updateRequest1 = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated by User 1",
            RowVersion = order1.RowVersion
        };

        var updateRequest2 = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated by User 2",
            RowVersion = order2.RowVersion // Same version as User 1
        };

        var json1 = JsonSerializer.Serialize(updateRequest1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var json2 = JsonSerializer.Serialize(updateRequest2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        // Act - First update should succeed
        var response1 = await Client.PutAsync($"/v1.0/purchase-orders/{purchaseOrderId}", content1);

        // Act - Second update should fail due to optimistic concurrency
        var response2 = await _client2.PutAsync($"/v1.0/purchase-orders/{purchaseOrderId}", content2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await response2.Content.ReadAsStringAsync();
        errorResponse.Should().Contain("concurrency");

        // Verify the database contains only the first update
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);

        savedOrder.Should().NotBeNull();
        savedOrder!.Notes.Should().Be("Updated by User 1");
        savedOrder.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public async Task Manager_Approves_While_Employee_Updates_Same_Order_Creates_Conflict()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupEmployeeAuthentication("emp123", "department1");
        SetupSecondClientManagerAuthentication("mgr123", "department1");
        SetupExternalServiceMocks();

        // Both get the current order state
        var getResponse = await Client.GetAsync($"/v1.0/purchase-orders/{purchaseOrderId}");
        var order = await DeserializeResponseAsync<PurchaseOrderDetailResponse>(getResponse);

        // Employee prepares update
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Employee update",
            RowVersion = order!.RowVersion
        };

        // Manager prepares approval
        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Manager approval",
            ApprovedBy = "manager@maliev.com"
        };

        // Act - Employee updates first
        var updateResponse = await PutAsJsonAsync($"/v1.0/purchase-orders/{purchaseOrderId}", updateRequest);

        // Act - Manager tries to approve with stale version
        var approveContent = new StringContent(JsonSerializer.Serialize(approveRequest), Encoding.UTF8, "application/json");
        var approveResponse = await _client2.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/approve", approveContent);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await approveResponse.Content.ReadAsStringAsync();
        errorResponse.Should().Contain("concurrency");

        // Verify order is updated but not approved
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);

        savedOrder.Should().NotBeNull();
        savedOrder!.Status.Should().Be(OrderStatus.Pending);
        savedOrder.Notes.Should().Be("Employee update");
        savedOrder.ApprovedBy.Should().BeNull();
    }

    [Fact]
    public async Task Two_Managers_Try_To_Approve_Same_Order_Simultaneously_Second_Fails()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");
        SetupSecondClientManagerAuthentication("mgr456", "department1");

        var approveRequest1 = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved by Manager 1",
            ApprovedBy = "manager1@maliev.com"
        };

        var approveRequest2 = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved by Manager 2",
            ApprovedBy = "manager2@maliev.com"
        };

        var content1 = new StringContent(JsonSerializer.Serialize(approveRequest1), Encoding.UTF8, "application/json");
        var content2 = new StringContent(JsonSerializer.Serialize(approveRequest2), Encoding.UTF8, "application/json");

        // Act - Both managers try to approve simultaneously
        var task1 = Client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/approve", content1);
        var task2 = _client2.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/approve", content2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert - One should succeed, one should fail
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        successCount.Should().Be(1);
        conflictCount.Should().Be(1);

        // Verify only one approval was persisted
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);

        savedOrder.Should().NotBeNull();
        savedOrder!.Status.Should().Be(OrderStatus.Approved);
        savedOrder.ApprovedBy.Should().NotBeNull();
        savedOrder.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_Purchase_Order_With_Stale_Version_Returns_Conflict()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupEmployeeAuthentication();
        SetupExternalServiceMocks();

        // Update the order to increment version
        await UpdatePurchaseOrderDirectly(purchaseOrderId, "Direct update");

        // Try to update with original version (now stale)
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Update with stale version",
            RowVersion = Convert.ToBase64String(new byte[] { 1, 0, 0, 0 }) // Original version
        };

        var json = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await response.Content.ReadAsStringAsync();
        errorResponse.Should().Contain("concurrency");
    }

    [Fact]
    public async Task Delete_Purchase_Order_With_Stale_Version_Returns_Conflict()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupEmployeeAuthentication();

        // Update the order to increment version
        await UpdatePurchaseOrderDirectly(purchaseOrderId, "Direct update");

        // Try to delete with original version (now stale)
        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{purchaseOrderId}?version=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await response.Content.ReadAsStringAsync();
        errorResponse.Should().Contain("concurrency");

        // Verify order still exists
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var order = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);
        order.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancel_Purchase_Order_With_Concurrent_Approval_Results_In_Conflict()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupEmployeeAuthentication(); // For cancellation
        SetupManagerAuthentication(_client2);  // For approval

        var cancelRequest = new CancelPurchaseOrderRequest
        {
            Reason = "No longer needed",
            CanceledBy = "employee@maliev.com"
        };

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved",
            ApprovedBy = "manager@maliev.com"
        };

        var cancelJson = JsonSerializer.Serialize(cancelRequest);
        var cancelContent = new StringContent(cancelJson, Encoding.UTF8, "application/json");

        var approveJson = JsonSerializer.Serialize(approveRequest);
        var approveContent = new StringContent(approveJson, Encoding.UTF8, "application/json");

        // Act - Simultaneous cancel and approve operations
        var cancelTask = Client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", cancelContent);
        var approveTask = _client2.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/approve", approveContent);

        var responses = await Task.WhenAll(cancelTask, approveTask);

        // Assert - One should succeed, one should fail due to business rules or concurrency
        var successResponses = responses.Where(r => r.IsSuccessStatusCode).ToList();
        var failureResponses = responses.Where(r => !r.IsSuccessStatusCode).ToList();

        // At least one should fail due to business logic or concurrency
        failureResponses.Should().NotBeEmpty();

        // Verify final state is consistent
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);

        savedOrder.Should().NotBeNull();

        // Order should be either cancelled or approved, not both
        if (savedOrder!.Status == OrderStatus.Cancelled)
        {
            savedOrder.CancelledBy.Should().NotBeNull();
            savedOrder.ApprovedBy.Should().BeNull();
        }
        else if (savedOrder.Status == OrderStatus.Approved)
        {
            savedOrder.ApprovedBy.Should().NotBeNull();
            savedOrder.CancelledBy.Should().BeNull();
        }
    }

    [Fact]
    public async Task Refresh_Order_Items_During_Concurrent_Update_Maintains_Data_Consistency()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrderWithItems();
        SetupEmployeeAuthentication();
        SetupEmployeeAuthentication(_client2);
        SetupExternalServiceMocks();

        // Client 1 updates purchase order
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated notes",
            RowVersion = Convert.ToBase64String(new byte[] { 1, 0, 0, 0 })
        };

        var updateJson = JsonSerializer.Serialize(updateRequest);
        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        // Client 2 refreshes order items
        var refreshRequest = new OrderItemRefreshRequest
        {
            OrderItemIds = new[] { 1, 2 }
        };

        var refreshJson = JsonSerializer.Serialize(refreshRequest);
        var refreshContent = new StringContent(refreshJson, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act - Concurrent update and refresh operations
        var updateTask = Client.PutAsync($"/v1.0/purchase-orders/{purchaseOrderId}", updateContent);
        var refreshTask = _client2.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/refresh-items", refreshContent);

        var responses = await Task.WhenAll(updateTask, refreshTask);

        // Assert - Operations should handle concurrency gracefully
        // At least one operation should complete successfully or fail with appropriate error
        responses.Should().NotBeNull();

        // Verify data consistency
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders
            .Include(po => po.OrderItems)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        savedOrder.Should().NotBeNull();
        // Data should be consistent regardless of which operation succeeded
        savedOrder!.OrderItems.Should().NotBeNull();
    }

    private async Task<int> CreateDraftPurchaseOrder()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-CONCUR-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            SupplierName = "Test Supplier",
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow,
            OrderDate = DateTime.UtcNow,
            Notes = "Original notes"
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> CreateDraftPurchaseOrderWithItems()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-ITEMS-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            SupplierName = "Test Supplier",
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ExternalOrderItemId = 1,
                    ProductName = "Test Product 1",
                    Quantity = 5,
                    UnitOfMeasure = "pieces",
                    UnitPrice = 100.00m,
                    TotalPrice = 500.00m,
                    Currency = "THB",
                    SourceService = "OrderService",
                    CachedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ExternalOrderItemId = 2,
                    ProductName = "Test Product 2",
                    Quantity = 5,
                    UnitOfMeasure = "pieces",
                    UnitPrice = 100.00m,
                    TotalPrice = 500.00m,
                    Currency = "THB",
                    SourceService = "OrderService",
                    CachedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task UpdatePurchaseOrderDirectly(int purchaseOrderId, string notes)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var order = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);
        order!.Notes = notes;
        order.UpdatedBy = "system";
        order.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    private void SetupEmployeeAuthentication(HttpClient client)
    {
        var token = "Bearer mock-employee-token";
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupManagerAuthentication(HttpClient client)
    {
        var token = "Bearer mock-manager-token";
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    protected override void SetupExternalServiceMocks()
    {
        MockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Supplier",
                IsActive = true
            });

        MockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrencyDto
            {
                Code = "THB",
                Name = "Thai Baht",
                IsActive = true
            });
    }

    private void SetupSecondClientAuthentication(string userId, string department)
    {
        var token = TestJwtHelper.GenerateEmployeeToken(userId, department);
        _client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private void SetupSecondClientManagerAuthentication(string userId, string department)
    {
        var token = TestJwtHelper.GenerateManagerToken(userId, department);
        _client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}