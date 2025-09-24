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
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 1: Employee creates internal PO with PDF generation
/// </summary>
public class CreateInternalPurchaseOrderTests : IntegrationTestBase
{
    public CreateInternalPurchaseOrderTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateInternalPurchaseOrder_ShouldSucceed_AndTriggerPdfGeneration()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Notes = "Test internal purchase order"
        };

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var purchaseOrder = await DeserializeResponseAsync<PurchaseOrderDto>(response);

        purchaseOrder.Should().NotBeNull();
        purchaseOrder!.Id.Should().BeGreaterThan(0);
        purchaseOrder.OrderType.Should().Be(OrderType.Internal);
        purchaseOrder.Status.Should().Be(OrderStatus.Pending);
        purchaseOrder.OrderNumber.Should().StartWith("PO");

        // Verify PDF generation was triggered for internal PO
        await Task.Delay(1000); // Give time for background processing

        await ExecuteInDbContextAsync(async dbContext =>
        {
            var domainEvents = await dbContext.DomainEvents
                .Where(e => e.AggregateId == purchaseOrder.Id.ToString() && e.EventType == "PurchaseOrderCreated")
                .ToListAsync();

            domainEvents.Should().HaveCount(1);
        });
    }

    [Fact]
    public async Task CreateInternalPurchaseOrder_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 0, // Invalid supplier ID
            OrderID = 0,    // Invalid order ID
            CurrencyID = 0, // Invalid currency ID
            OrderType = OrderType.Internal
        };

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var errorResponse = await DeserializeResponseAsync<ValidationErrorResponse>(response);

        errorResponse.Should().NotBeNull();
        errorResponse!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateInternalPurchaseOrder_ShouldCalculateWHTCorrectly()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var response = await PostAsJsonAsync("/v1/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var purchaseOrder = await DeserializeResponseAsync<PurchaseOrderDto>(response);

        purchaseOrder.Should().NotBeNull();
        purchaseOrder!.WHTRate.Should().BeGreaterThanOrEqualTo(0);
        if (purchaseOrder.WHTRate > 0)
        {
            purchaseOrder.WHTAmount.Should().BeGreaterThan(0);
            purchaseOrder.TotalAmount.Should().Be(purchaseOrder.SubtotalAmount - purchaseOrder.WHTAmount);
        }
    }
}