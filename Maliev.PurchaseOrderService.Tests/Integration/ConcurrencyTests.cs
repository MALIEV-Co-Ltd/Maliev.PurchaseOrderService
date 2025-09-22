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

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 3: Optimistic concurrency handling
/// </summary>
public class ConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace with in-memory database for testing
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PurchaseOrderContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForConcurrencyTesting");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithConcurrencyConflict_ShouldReturnConflict()
    {
        // Arrange - Create a purchase order
        using var scope1 = _factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2025-CONCURRENCY-001",
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            SupplierName = "Test Supplier",
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderType = OrderType.Internal,
            SubtotalAmount = 10000m,
            TotalAmount = 10000m,
            CreatedBy = "employee1",
            CreatedAt = DateTime.UtcNow
        };

        context1.PurchaseOrders.Add(purchaseOrder);
        await context1.SaveChangesAsync();

        // Get the initial row version
        var initialPO = await context1.PurchaseOrders.FirstAsync(p => p.Id == purchaseOrder.Id);
        var originalRowVersion = initialPO.RowVersion;

        // Simulate first user updating the purchase order
        using var scope2 = _factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var firstUpdate = await context2.PurchaseOrders.FirstAsync(p => p.Id == purchaseOrder.Id);
        firstUpdate.Notes = "Updated by first user";
        await context2.SaveChangesAsync();

        // Act - Second user tries to update with the original row version
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated by second user",
            RowVersion = originalRowVersion != null ? Convert.ToBase64String(originalRowVersion) : string.Empty // Stale row version
        };

        var json = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        var response = await _client.PutAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("concurrency");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithCorrectRowVersion_ShouldSucceed()
    {
        // Arrange - Create a purchase order
        using var scope1 = _factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2025-CONCURRENCY-002",
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            SupplierName = "Test Supplier",
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderType = OrderType.Internal,
            SubtotalAmount = 10000m,
            TotalAmount = 10000m,
            CreatedBy = "employee1",
            CreatedAt = DateTime.UtcNow
        };

        context1.PurchaseOrders.Add(purchaseOrder);
        await context1.SaveChangesAsync();

        // Get the current row version
        var currentPO = await context1.PurchaseOrders.FirstAsync(p => p.Id == purchaseOrder.Id);
        var currentRowVersion = currentPO.RowVersion;

        // Act - Update with correct row version
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated with correct row version",
            RowVersion = currentRowVersion != null ? Convert.ToBase64String(currentRowVersion) : string.Empty
        };

        var json = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        var response = await _client.PutAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var updatedPO = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        updatedPO.Should().NotBeNull();
        updatedPO!.Notes.Should().Be("Updated with correct row version");
        updatedPO.RowVersion.Should().NotBe(currentRowVersion != null ? Convert.ToBase64String(currentRowVersion) : string.Empty); // Row version should have changed
    }

    [Fact]
    public async Task SimultaneousApprovalAndCancellation_ShouldHandleConcurrency()
    {
        // Arrange - Create a purchase order
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2025-CONCURRENCY-003",
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            SupplierName = "Test Supplier",
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderType = OrderType.Internal,
            SubtotalAmount = 10000m,
            TotalAmount = 10000m,
            CreatedBy = "employee1",
            CreatedAt = DateTime.UtcNow
        };

        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        // Create two clients for simultaneous operations
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

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

        var approvalJson = JsonSerializer.Serialize(approvalRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var cancellationJson = JsonSerializer.Serialize(cancellationRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var approvalContent = new StringContent(approvalJson, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        var cancellationContent = new StringContent(cancellationJson, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act - Simulate simultaneous approval and cancellation
        var approvalTask = client1.PostAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}/approve", approvalContent);
        var cancellationTask = client2.PostAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}/cancel", cancellationContent);

        var results = await Task.WhenAll(approvalTask, cancellationTask);

        // Assert - One should succeed, one should fail
        var successCount = results.Count(r => r.IsSuccessStatusCode);
        var conflictCount = results.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict);

        successCount.Should().Be(1);
        conflictCount.Should().Be(1);

        // Verify final state is consistent
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var finalPO = await verifyContext.PurchaseOrders.FirstAsync(p => p.Id == purchaseOrder.Id);
        finalPO.Status.Should().BeOneOf(OrderStatus.Approved, OrderStatus.Cancelled);
    }

    [Fact]
    public async Task BulkOperations_WithConcurrency_ShouldMaintainDataIntegrity()
    {
        // Arrange - Create multiple purchase orders
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrders = new List<PurchaseOrder>();
        for (int i = 1; i <= 5; i++)
        {
            purchaseOrders.Add(new PurchaseOrder
            {
                OrderNumber = $"PO-2025-BULK-{i:D3}",
                SupplierID = 1,
                OrderID = i,
                CurrencyID = 1,
                SupplierName = "Test Supplier",
                CurrencyCode = "THB",
                CurrencySymbol = "฿",
                Currency = "THB",
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderType = OrderType.Internal,
                SubtotalAmount = 1000m * i,
                TotalAmount = 1000m * i,
                CreatedBy = "employee1",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.PurchaseOrders.AddRange(purchaseOrders);
        await context.SaveChangesAsync();

        // Act - Perform concurrent updates on different orders
        var updateTasks = purchaseOrders.Select(async (po, index) =>
        {
            var updateRequest = new UpdatePurchaseOrderRequest
            {
                Notes = $"Bulk update {index + 1}"
            };

            var json = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

            return await _client.PutAsync($"/purchaseorders/api/purchase-orders/{po.Id}", content);
        });

        var responses = await Task.WhenAll(updateTasks);

        // Assert - All updates should succeed (no concurrency conflicts between different orders)
        responses.Should().AllSatisfy(response =>
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK));

        // Verify all orders were updated correctly
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var updatedOrders = await verifyContext.PurchaseOrders
            .Where(po => purchaseOrders.Select(p => p.Id).Contains(po.Id))
            .ToListAsync();

        updatedOrders.Should().HaveCount(5);
        updatedOrders.Should().AllSatisfy(po =>
            po.Notes.Should().StartWith("Bulk update"));
    }
}