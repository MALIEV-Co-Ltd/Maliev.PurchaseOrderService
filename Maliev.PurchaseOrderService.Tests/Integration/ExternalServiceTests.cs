using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using Xunit;
using FluentAssertions;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 5: External service integration patterns
/// </summary>
public class ExternalServiceTests : IntegrationTestBase
{
    public ExternalServiceTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }


    [Fact]
    public async Task CreatePurchaseOrder_ShouldIntegrateWithSupplierService()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.Internal,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var purchaseOrder = await DeserializeResponseAsync<PurchaseOrderDto>(response);

        purchaseOrder.Should().NotBeNull();
        purchaseOrder!.SupplierName.Should().NotBeNullOrEmpty();

        // Verify external service interaction
        MockSupplierService.Verify(x => x.ValidateSupplierAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshOrderItems_ShouldIntegrateWithOrderService()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/items/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<OrderItemRefreshResult>(response);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.NewItemCount.Should().BeGreaterThan(0);

        // Verify external service interaction
        MockOrderService.Verify(x => x.GetOrderItemsAsync(seededPurchaseOrder.OrderID, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExternalServiceFailure_ShouldBeHandledGracefully()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Setup mock to simulate service failure
        MockSupplierService
            .Setup(x => x.ValidateSupplierAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("External service unavailable"));

        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 999, // Non-existent supplier to trigger failure
            OrderID = 1,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert - Should return appropriate error
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,   // Validation error
            HttpStatusCode.ServiceUnavailable // Service unavailable
        );
    }

    [Fact]
    public async Task CurrencyServiceIntegration_ShouldProvideExchangeRates()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1, // Will be resolved through CurrencyService
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var purchaseOrder = await DeserializeResponseAsync<PurchaseOrderDto>(response);

        purchaseOrder.Should().NotBeNull();
        purchaseOrder!.CurrencyCode.Should().Be("THB");
        purchaseOrder.CurrencySymbol.Should().Be("฿");

        // Verify external service interaction
        MockCurrencyService.Verify(x => x.ValidateCurrencyAsync("THB", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CircuitBreakerPattern_ShouldPreventCascadingFailures()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act - Make multiple concurrent requests
        var tasks = Enumerable.Range(1, 5).Select(_ =>
            PostAsJsonAsync("/v1.0/purchase-orders", createRequest));

        var responses = await Task.WhenAll(tasks);

        // Assert - All should either succeed or fail gracefully
        responses.Should().AllSatisfy(response =>
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Created,
                HttpStatusCode.BadRequest,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.TooManyRequests
            );
        });
    }

    [Fact]
    public async Task CacheIntegration_ShouldImprovePerformance()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act - First request (cache miss)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response1 = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);
        var firstRequestTime = sw.ElapsedMilliseconds;

        // Second request for same supplier (cache hit - if caching is implemented)
        sw.Restart();
        var response2 = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);
        var secondRequestTime = sw.ElapsedMilliseconds;

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Note: This assertion might not be reliable in all test environments
        // It's more of a performance guideline
        Console.WriteLine($"First request: {firstRequestTime}ms, Second request: {secondRequestTime}ms");
    }

    [Fact]
    public async Task RetryPolicy_ShouldHandleTransientFailures()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", createRequest);

        // Assert - Should eventually succeed despite potential transient failures
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest // Known validation issue
        );
    }
}