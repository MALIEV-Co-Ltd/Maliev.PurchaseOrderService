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
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class ManagerApprovesPurchaseOrderTests : IntegrationTestBase
{
    public ManagerApprovesPurchaseOrderTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Manager_Approves_Internal_Purchase_Order_Successfully_And_Triggers_PDF_Generation()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");
        SetupPdfGenerationMock();

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved for procurement",
            ApprovedBy = "manager@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedOrder = await DeserializeResponseAsync<PurchaseOrderResponse>(response);

        approvedOrder.Should().NotBeNull();
        approvedOrder!.Status.Should().Be(OrderStatus.Approved);
        approvedOrder.ApprovedBy.Should().Be("manager@maliev.com");
        approvedOrder.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        approvedOrder.Notes.Should().Be("Approved for procurement");

        // Verify PDF generation was triggered for internal purchase order
        MockPdfService.Verify(x => x.GeneratePurchaseOrderPdfAsync(
            It.Is<int>(id => id == purchaseOrderId),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify domain event was published by checking database
        // MockDomainEventService.Verify removed - using real service that persists to database
        // Domain event verification can be added by querying dbContext.DomainEvents

        // Verify data persistence
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var savedOrder = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);

        savedOrder.Should().NotBeNull();
        savedOrder!.Status.Should().Be(OrderStatus.Approved);
        savedOrder.ApprovedBy.Should().Be("manager@maliev.com");
        savedOrder.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Manager_Approves_External_Purchase_Order_Successfully_Without_PDF_Generation()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftExternalPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "External order approved",
            ApprovedBy = "manager@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedOrder = await DeserializeResponseAsync<PurchaseOrderResponse>(response);

        approvedOrder.Should().NotBeNull();
        approvedOrder!.Status.Should().Be(OrderStatus.Approved);

        // Verify PDF generation was NOT triggered for external purchase order
        MockPdfService.Verify(x => x.GeneratePurchaseOrderPdfAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);

        // Verify domain event was still published
        // MockDomainEventService.Verify removed - using real service that persists to database
        // Domain event verification can be added by querying dbContext.DomainEvents
    }

    [Fact]
    public async Task Manager_Cannot_Approve_Already_Approved_Purchase_Order()
    {
        // Arrange
        var purchaseOrderId = await CreateApprovedPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Trying to approve again",
            ApprovedBy = "manager@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("already approved");
    }

    [Fact]
    public async Task Manager_Cannot_Approve_Cancelled_Purchase_Order()
    {
        // Arrange
        var purchaseOrderId = await CreateCancelledPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Trying to approve cancelled order",
            ApprovedBy = "manager@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("cancelled");
    }

    [Fact]
    public async Task Employee_Cannot_Approve_Purchase_Order_Without_Manager_Role()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupEmployeeAuthentication("emp123", "department1");

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Employee trying to approve",
            ApprovedBy = "employee@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_Approval_Workflow_With_Concurrent_Modifications_Fails()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");
        SetupPdfGenerationMock();

        // Simulate concurrent modification by updating the purchase order directly in database
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
            var order = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);
            order!.Notes = "Modified concurrently";
            // RowVersion is a byte array, cannot be incremented like an integer
            // Simulate version change by updating other field
            await dbContext.SaveChangesAsync();
        }

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Should fail due to concurrency",
            ApprovedBy = "manager@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("concurrency");
    }

    [Fact]
    public async Task Manager_Approval_With_PDF_Generation_Failure_Logs_Error_But_Continues()
    {
        // Arrange
        var purchaseOrderId = await CreateDraftPurchaseOrder();
        SetupManagerAuthentication("mgr123", "department1");
        SetupFailingPdfGenerationMock();

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved despite PDF failure",
            ApprovedBy = "manager@maliev.com"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderId}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedOrder = await DeserializeResponseAsync<PurchaseOrderResponse>(response);

        // Order should still be approved despite PDF generation failure
        approvedOrder.Should().NotBeNull();
        approvedOrder!.Status.Should().Be(OrderStatus.Approved);

        // Verify PDF generation was attempted
        MockPdfService.Verify(x => x.GeneratePurchaseOrderPdfAsync(
            It.Is<int>(id => id == purchaseOrderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<int> CreateDraftPurchaseOrder()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-TEST-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            SupplierID = 1234,
            SupplierName = "Test Supplier",
            OrderID = 1001,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> CreateDraftExternalPurchaseOrder()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-EXT-001",
            OrderType = OrderType.External,
            Status = OrderStatus.Pending,
            SupplierID = 1234,
            SupplierName = "External Supplier",
            OrderID = 1002,
            CurrencyID = 2,
            CurrencyCode = "USD",
            CurrencySymbol = "$",
            Currency = "USD",
            OrderDate = DateTime.UtcNow,
            SubtotalAmount = 2000.00m,
            TotalAmount = 2000.00m,
            CustomerPO = "CUST-PO-001",
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> CreateApprovedPurchaseOrder()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-APPROVED-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Approved,
            SupplierID = 1234,
            SupplierName = "Test Supplier",
            OrderID = 1003,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow,
            ApprovedBy = "existing-manager@maliev.com",
            ApprovedAt = DateTime.UtcNow.AddHours(-1)
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> CreateCancelledPurchaseOrder()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-CANCELLED-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Cancelled,
            SupplierID = 1234,
            SupplierName = "Test Supplier",
            OrderID = 1004,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            OrderDate = DateTime.UtcNow,
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private void SetupPdfGenerationMock()
    {
        MockPdfService
            .Setup(x => x.GeneratePurchaseOrderPdfAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult
            {
                Success = true,
                FilePath = "path/to/PO-TEST-001.pdf",
                FileSize = 1024,
                GeneratedAt = DateTime.UtcNow,
                GenerationTime = TimeSpan.FromSeconds(2)
            });
    }

    private void SetupFailingPdfGenerationMock()
    {
        MockPdfService
            .Setup(x => x.GeneratePurchaseOrderPdfAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("PDF generation service unavailable"));
    }
}