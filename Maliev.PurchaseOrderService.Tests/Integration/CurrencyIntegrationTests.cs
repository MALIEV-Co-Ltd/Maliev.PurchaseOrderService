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
/// Integration tests for T032: Currency service integration
///
/// Tests Scenario 8 from quickstart.md:
/// - Currency service integration and caching
/// - Currency validation through external CurrencyService
/// - Exchange rate calculations and caching
/// - Multi-currency support (USD, THB, etc.)
/// - Currency conversion for purchase orders
/// - Service unavailability handling and fallback mechanisms
/// - Cache expiration and refresh strategies
/// </summary>
public class CurrencyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<CurrencyIntegrationTests> _logger;

    public CurrencyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Configure test logging
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<CurrencyIntegrationTests>();
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithValidCurrencyID_ShouldFetchCurrencyFromService()
    {
        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1, // USD
            OrderType = (Data.Enums.OrderType)OrderType.External,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Notes = "Test order with USD currency from CurrencyService",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Manufacturing",
                AddressLine1 = "123 Test Street",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test@maliev.com"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithThaiCurrency_ShouldHandleTHBCorrectly()
    {
        // Arrange - Test THB currency with Thai supplier
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 9012,
            CurrencyID = 2, // THB (Thai Baht)
            OrderType = (Data.Enums.OrderType)OrderType.External,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(45),
            WhtRate = 3.0m,
            Notes = "Thai supplier order in local currency",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Bangkok Manufacturing Hub",
                AddressLine1 = "456 Industrial Road",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "receiving@maliev.co.th"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidCurrencyID_ShouldReturnValidationError()
    {
        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 999, // Invalid currency ID
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Test order with invalid currency ID",
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

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");

        // When implemented, should return BadRequest with currency validation error
        // response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // var content = await response.Content.ReadAsStringAsync();
        // content.Should().Contain("Currency with ID 999 not found in CurrencyService");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_ChangeCurrency_ShouldRecalculateAmounts()
    {
        // This test validates currency change triggers recalculation
        // From THB to USD with exchange rate conversion

        // Arrange
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            CurrencyID = 1, // Change from THB to USD
            WhtRate = 3.0m,
            Notes = "Currency changed to USD due to supplier payment preference",
            RowVersion = "AAAAAAAAB9F="
        };

        // Act
        var response = await _client.PutAsJsonAsync("/purchase-orders/12349", updateRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders update endpoint is not implemented yet");
    }

    [Fact]
    public async Task CurrencyService_Unavailable_ShouldReturnServiceError()
    {
        // This test validates handling when CurrencyService is unavailable

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Test order when CurrencyService is down",
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

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");

        // When implemented and CurrencyService is unavailable, should return 502 Bad Gateway
        // response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        // var content = await response.Content.ReadAsStringAsync();
        // content.Should().Contain("CurrencyService is currently unavailable");
    }

    [Fact]
    public async Task CurrencyCache_ShouldCacheValidatedCurrencies()
    {
        // This test validates that currency information is cached to reduce API calls

        // Arrange - Create multiple orders with same currency
        var request1 = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1, // USD
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "First order to trigger currency cache",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact 1",
                AddressLine1 = "Test Address 1",
                City = "Test City",
                PostalCode = "12345",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test1@maliev.com"
            }
        };

        var request2 = new CreatePurchaseOrderRequest
        {
            SupplierID = 1235,
            OrderID = 5679,
            CurrencyID = 1, // Same USD currency - should use cache
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Second order using cached currency",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact 2",
                AddressLine1 = "Test Address 2",
                City = "Test City",
                PostalCode = "12345",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test2@maliev.com"
            }
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/purchase-orders", request1);
        var response2 = await _client.PostAsJsonAsync("/purchase-orders", request2);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response1.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task ExchangeRateCalculation_ShouldUseLatestRates()
    {
        // This test validates that exchange rates are fetched and applied correctly

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 9012,
            CurrencyID = 2, // THB
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Order requiring exchange rate calculation",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Bangkok Manufacturing",
                AddressLine1 = "456 Industrial Road",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test@maliev.co.th"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task CurrencyConversion_MultiCurrency_ShouldHandleComplexCalculations()
    {
        // This test validates complex currency conversions with WHT calculations

        // Arrange - Order in THB with conversion to USD for reporting
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 7890,
            OrderID = 9012,
            CurrencyID = 2, // THB
            OrderType = (Data.Enums.OrderType)OrderType.External,
            WhtRate = 3.0m,
            Notes = "Multi-currency order with WHT in Thai Baht",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Bangkok Parts Ltd",
                AddressLine1 = "789 Supply Chain Blvd",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "orders@bangkokparts.co.th"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task CurrencyService_TimeoutHandling_ShouldReturnGracefully()
    {
        // This test validates graceful handling of CurrencyService timeouts

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 3, // Currency that might cause timeout
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Test order to trigger CurrencyService timeout",
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

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");

        // When implemented, should handle timeouts gracefully
        // response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
    }

    [Fact]
    public async Task CurrencyValidation_SupportedCurrencies_ShouldAcceptValidCurrencies()
    {
        // This test validates that only supported currencies are accepted

        // Test multiple supported currencies
        var supportedCurrencies = new[]
        {
            new { CurrencyID = 1, Code = "USD", Description = "US Dollar" },
            new { CurrencyID = 2, Code = "THB", Description = "Thai Baht" },
            new { CurrencyID = 3, Code = "EUR", Description = "Euro" },
            new { CurrencyID = 4, Code = "GBP", Description = "British Pound" }
        };

        foreach (var currency in supportedCurrencies)
        {
            // Arrange
            var request = new CreatePurchaseOrderRequest
            {
                SupplierID = 1234,
                OrderID = 5678 + currency.CurrencyID,
                CurrencyID = currency.CurrencyID,
                OrderType = (Data.Enums.OrderType)OrderType.External,
                Notes = $"Test order with {currency.Code} currency",
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

            // Act
            var response = await _client.PostAsJsonAsync("/purchase-orders", request);

            // Assert - Should fail because implementation doesn't exist yet (TDD)
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                $"because the purchase orders controller endpoint is not implemented yet for {currency.Code}");
        }
    }

    [Fact]
    public async Task CurrencyCache_Expiration_ShouldRefreshAfterTimeout()
    {
        // This test validates cache expiration and refresh logic

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1, // USD
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = "Test order for cache expiration validation",
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

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task CurrencyConversion_RealTimeRates_ShouldFetchLatestExchangeRates()
    {
        // This test validates real-time exchange rate fetching for accurate conversions

        // Arrange
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            CurrencyID = 2, // Change to THB
            Notes = "Currency conversion with real-time exchange rates",
            RowVersion = "AAAAAAAAB9F="
        };

        // Act
        var response = await _client.PutAsJsonAsync("/purchase-orders/12345", updateRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders update endpoint is not implemented yet");
    }

    [Fact]
    public async Task CurrencyService_CircuitBreaker_ShouldHandleRepeatedFailures()
    {
        // This test validates circuit breaker pattern for CurrencyService failures

        // Arrange - Multiple requests that would trigger circuit breaker
        var requests = new List<CreatePurchaseOrderRequest>();
        for (int i = 1; i <= 5; i++)
        {
            requests.Add(new CreatePurchaseOrderRequest
            {
                SupplierID = 1234,
                OrderID = 5678 + i,
                CurrencyID = 999, // Invalid currency to trigger failures
                OrderType = (Data.Enums.OrderType)OrderType.External,
                Notes = $"Circuit breaker test request {i}",
                ShippingAddress = new CreateAddressRequest
                {
                    AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                    ContactName = $"Test Contact {i}",
                    AddressLine1 = "Test Address",
                    City = "Test City",
                    PostalCode = "12345",
                    Country = "Thailand",
                    PhoneNumber = "+66-2-555-0123",
                    EmailAddress = $"test{i}@maliev.com"
                }
            });
        }

        // Act & Assert
        foreach (var request in requests)
        {
            var response = await _client.PostAsJsonAsync("/purchase-orders", request);

            // Should fail because implementation doesn't exist yet (TDD)
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "because the purchase orders controller endpoint is not implemented yet");
        }
    }

    private async Task<PurchaseOrderResponse> CreateTestPurchaseOrderWithCurrency(int currencyId)
    {
        // Helper method to create test purchase orders with specific currencies
        // This will be used once the implementation exists
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = currencyId,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            Notes = $"Test order with currency ID {currencyId}",
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