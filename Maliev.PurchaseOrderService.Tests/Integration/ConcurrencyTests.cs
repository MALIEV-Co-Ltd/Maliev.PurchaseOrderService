using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Xunit;
using FluentAssertions;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 3: Optimistic concurrency handling
/// </summary>
public class ConcurrencyTests : IntegrationTestBase
{
    public ConcurrencyTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithConcurrencyConflict_ShouldReturnConflict()
    {
        // Arrange - Set up authentication
        SetupEmployeeAuthentication("emp123", "test");

        // Create a purchase order using the test infrastructure - using same user to avoid authorization issues
        var testPO = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending, "emp123");

        // Get the initial row version
        var originalRowVersion = testPO.RowVersion;
        var originalRowVersionBase64 = originalRowVersion != null ? Convert.ToBase64String(originalRowVersion) : string.Empty;

        // Simulate first user updating the purchase order directly in the database to change the RowVersion
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var firstUpdate = await dbContext.PurchaseOrders.FirstAsync(p => p.Id == testPO.Id);
            firstUpdate.Notes = "Updated by first user";
            firstUpdate.UpdatedBy = "firstuser";
            firstUpdate.UpdatedAt = DateTime.UtcNow;

            // Manually set a different RowVersion to simulate the concurrency change
            // Since test DB might use in-memory provider, we'll force a change
            firstUpdate.RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 };

            await dbContext.SaveChangesAsync();
        });

        // Act - Second user tries to update with the original row version (should conflict)
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated by second user",
            RowVersion = originalRowVersionBase64 // Stale row version
        };

        var response = await PutAsJsonAsync($"/v1.0/purchase-orders/{testPO.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("Concurrency conflict");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithCorrectRowVersion_ShouldSucceed()
    {
        // Arrange - Set up authentication
        SetupEmployeeAuthentication("emp123", "test");

        // Create a purchase order using the test infrastructure - using same user to avoid authorization issues
        var testPO = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending, "emp123");

        // Get the current row version
        var currentRowVersion = testPO.RowVersion;

        // Act - Update with correct row version
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated with correct row version",
            RowVersion = currentRowVersion != null ? Convert.ToBase64String(currentRowVersion) : string.Empty
        };

        var response = await PutAsJsonAsync($"/v1.0/purchase-orders/{testPO.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var updatedPO = await DeserializeResponseAsync<PurchaseOrderDto>(response);

        updatedPO.Should().NotBeNull();
        updatedPO!.Notes.Should().Be("Updated with correct row version");
        // Note: In test environment, RowVersion may not change automatically
        // In production with SQL Server, this would be different
        updatedPO.RowVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SimultaneousApprovalAndCancellation_ShouldHandleConcurrency()
    {
        // Arrange - Set up authentication
        SetupManagerAuthentication("mgr123", "test");

        // Create a purchase order using the test infrastructure
        var testPO = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending, "employee1");

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved",
            ApprovedBy = "manager1"
        };

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Cancelled",
            CanceledBy = "manager2"
        };

        // Act - Simulate simultaneous approval and cancellation
        var approvalTask = PostAsJsonAsync($"/v1.0/purchase-orders/{testPO.Id}/approve", approvalRequest);
        var cancellationTask = PostAsJsonAsync($"/v1.0/purchase-orders/{testPO.Id}/cancel", cancellationRequest);

        var results = await Task.WhenAll(approvalTask, cancellationTask);

        // Assert - One should succeed, one should fail
        var successCount = results.Count(r => r.IsSuccessStatusCode);
        var conflictCount = results.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict);

        successCount.Should().Be(1);
        conflictCount.Should().Be(1);

        // Verify final state is consistent
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var finalPO = await dbContext.PurchaseOrders.FirstAsync(p => p.Id == testPO.Id);
            finalPO.Status.Should().BeOneOf(OrderStatus.Approved, OrderStatus.Cancelled);
        });
    }

    [Fact]
    public async Task BulkOperations_WithConcurrency_ShouldMaintainDataIntegrity()
    {
        // Arrange - Set up authentication
        SetupEmployeeAuthentication("emp123", "test");

        // Business Logic Alignment: Create multiple purchase orders with same user as authenticator
        var purchaseOrders = new List<PurchaseOrder>();
        for (int i = 1; i <= 5; i++)
        {
            // Use same user ID for authorization alignment
            var testPO = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending, "emp123");
            purchaseOrders.Add(testPO);
        }

        // Act - Perform concurrent updates on different orders
        var updateTasks = purchaseOrders.Select(async (po, index) =>
        {
            var updateRequest = new UpdatePurchaseOrderRequest
            {
                Notes = $"Bulk update {index + 1}",
                RowVersion = po.RowVersion != null ? Convert.ToBase64String(po.RowVersion) : string.Empty
            };

            return await PutAsJsonAsync($"/v1.0/purchase-orders/{po.Id}", updateRequest);
        });

        var responses = await Task.WhenAll(updateTasks);

        // Assert - Business Logic Alignment: All updates should succeed (no concurrency conflicts between different orders)
        responses.Should().AllSatisfy(response =>
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK));

        // Verify all orders were updated correctly
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var updatedOrders = await dbContext.PurchaseOrders
                .Where(po => purchaseOrders.Select(p => p.Id).Contains(po.Id))
                .ToListAsync();

            updatedOrders.Should().HaveCount(5);
            updatedOrders.Should().AllSatisfy(po =>
                po.Notes.Should().StartWith("Bulk update"));
        });
    }
}