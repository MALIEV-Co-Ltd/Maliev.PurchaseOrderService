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
/// Integration test Scenario 2: Manager approves purchase order
/// </summary>
public class ApprovalWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApprovalWorkflowTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("InMemoryDbForApprovalTesting");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithValidManager_ShouldSucceed()
    {
        // Arrange - Create a purchase order first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2025-001",
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
            TotalAmount = 9700m,
            WHTRate = 3m,
            WHTAmount = 300m,
            CreatedBy = "employee1",
            CreatedAt = DateTime.UtcNow
        };

        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved by manager",
            ApprovedBy = "manager1"
        };

        var json = JsonSerializer.Serialize(approvalRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}/approve", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var approvedPO = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        approvedPO.Should().NotBeNull();
        approvedPO!.Status.Should().Be(OrderStatus.Approved);
        approvedPO.ApprovedBy.Should().Be("manager1");
        approvedPO.ApprovedAt.Should().NotBeNull();

        // Verify audit log entry was created
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == purchaseOrder.Id.ToString() && a.Action == AuditAction.Approve)
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
        auditLogs[0].UserId.Should().Be("manager1");
        auditLogs[0].ChangeReason.Should().Contain("Approved");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_AlreadyApproved_ShouldReturnConflict()
    {
        // Arrange - Create an already approved purchase order
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2025-002",
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            SupplierName = "Test Supplier",
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Approved, // Already approved
            OrderType = OrderType.Internal,
            SubtotalAmount = 10000m,
            TotalAmount = 9700m,
            CreatedBy = "employee1",
            CreatedAt = DateTime.UtcNow,
            ApprovedBy = "manager1",
            ApprovedAt = DateTime.UtcNow
        };

        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Second approval attempt",
            ApprovedBy = "manager2"
        };

        var json = JsonSerializer.Serialize(approvalRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}/approve", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("already approved");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_NonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approval for non-existent order",
            ApprovedBy = "manager1"
        };

        var json = JsonSerializer.Serialize(approvalRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/purchaseorders/api/purchase-orders/99999/approve", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_ShouldTriggerDomainEvent()
    {
        // Arrange - Create a purchase order
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2025-003",
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
            TotalAmount = 9700m,
            CreatedBy = "employee1",
            CreatedAt = DateTime.UtcNow
        };

        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved with event tracking",
            ApprovedBy = "manager1"
        };

        var json = JsonSerializer.Serialize(approvalRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/purchaseorders/api/purchase-orders/{purchaseOrder.Id}/approve", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Give time for domain event processing
        await Task.Delay(500);

        // Verify domain event was created
        var domainEvents = await context.DomainEvents
            .Where(e => e.AggregateId == purchaseOrder.Id.ToString() && e.EventType == "PurchaseOrderApproved")
            .ToListAsync();

        domainEvents.Should().HaveCount(1);
        domainEvents[0].UserId.Should().Be("manager1");
    }
}