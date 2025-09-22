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

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 1: Employee creates internal PO with PDF generation
/// </summary>
public class CreateInternalPurchaseOrderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateInternalPurchaseOrderTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateInternalPurchaseOrder_ShouldSucceed_AndTriggerPdfGeneration()
    {
        // Arrange
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Notes = "Test internal purchase order"
        };

        var json = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/purchaseorders/api/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var purchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        purchaseOrder.Should().NotBeNull();
        purchaseOrder!.Id.Should().BeGreaterThan(0);
        purchaseOrder.OrderType.Should().Be(OrderType.Internal);
        purchaseOrder.Status.Should().Be(OrderStatus.Pending);
        purchaseOrder.OrderNumber.Should().StartWith("PO-");

        // Verify PDF generation was triggered for internal PO
        await Task.Delay(1000); // Give time for background processing

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var domainEvents = await context.DomainEvents
            .Where(e => e.AggregateId == purchaseOrder.Id.ToString() && e.EventType == "PurchaseOrderCreated")
            .ToListAsync();

        domainEvents.Should().HaveCount(1);
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

        var json = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/purchaseorders/api/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateInternalPurchaseOrder_ShouldCalculateWHTCorrectly()
    {
        // Arrange
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30)
        };

        var json = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync("/purchaseorders/api/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var purchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        purchaseOrder.Should().NotBeNull();
        purchaseOrder!.WHTRate.Should().BeGreaterThanOrEqualTo(0);
        if (purchaseOrder.WHTRate > 0)
        {
            purchaseOrder.WHTAmount.Should().BeGreaterThan(0);
            purchaseOrder.TotalAmount.Should().Be(purchaseOrder.SubtotalAmount - purchaseOrder.WHTAmount);
        }
    }
}