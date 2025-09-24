using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Xunit;
using FluentAssertions;
using Moq;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 2: Manager approves purchase order
/// </summary>
public class ApprovalWorkflowTests : IntegrationTestBase
{
    public ApprovalWorkflowTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithValidManager_ShouldSucceed()
    {
        // Arrange
        SetupManagerAuthentication("manager1");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending);

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved by manager",
            ApprovedBy = "manager1"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedPO = await DeserializeResponseAsync<PurchaseOrderDto>(response);

        approvedPO.Should().NotBeNull();
        approvedPO!.Status.Should().Be(OrderStatus.Approved);
        approvedPO.ApprovedBy.Should().Be("manager1");
        approvedPO.ApprovedAt.Should().NotBeNull();

        // Verify audit log entry was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var auditLogs = await dbContext.AuditLogs
                .Where(a => a.EntityId == seededPurchaseOrder.Id.ToString() && a.Action == AuditAction.Approve)
                .ToListAsync();

            auditLogs.Should().HaveCount(1);
            auditLogs[0].UserId.Should().Be("manager1");
            auditLogs[0].ChangeReason.Should().Contain("Approved");
        });
    }

    [Fact]
    public async Task ApprovePurchaseOrder_AlreadyApproved_ShouldReturnConflict()
    {
        // Arrange
        SetupManagerAuthentication("manager2");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Approved);

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Second approval attempt",
            ApprovedBy = "manager2"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("already approved");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_NonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        SetupManagerAuthentication("manager1");
        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approval for non-existent order",
            ApprovedBy = "manager1"
        };

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders/99999/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_ShouldTriggerDomainEvent()
    {
        // Arrange
        SetupManagerAuthentication("manager1");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending);

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved with event tracking",
            ApprovedBy = "manager1"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1/purchase-orders/{seededPurchaseOrder.Id}/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Give time for domain event processing
        await Task.Delay(500);

        // Verify domain event was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var domainEvents = await dbContext.DomainEvents
                .Where(e => e.AggregateId == seededPurchaseOrder.Id.ToString() && e.EventType == "PurchaseOrderApproved")
                .ToListAsync();

            domainEvents.Should().HaveCount(1);
            domainEvents[0].UserId.Should().Be("manager1");

            // Verify domain event service was called by checking database
            // MockDomainEventService.Verify removed - using real service that persists to database
        });
    }
}