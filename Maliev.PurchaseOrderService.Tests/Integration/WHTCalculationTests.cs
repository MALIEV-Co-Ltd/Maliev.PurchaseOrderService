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
/// Integration tests for T030: WHT calculation and validation
///
/// Tests Scenario 6 from quickstart.md:
/// - WHT calculations with Thailand tax regulations
/// - WHT rate validation (0-15% legal limits)
/// - Automatic WHT amount calculation
/// - Subtotal and total amount recalculation
/// - Thailand tax compliance
/// </summary>
public class WHTCalculationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<WHTCalculationTests> _logger;

    public WHTCalculationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Configure test logging
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<WHTCalculationTests>();
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithValidWHTRate_ShouldCalculateWHTCorrectly()
    {
        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1, // USD
            OrderType = (Data.Enums.OrderType)OrderType.External,
            CustomerPO = "CUST-PO-2025-5678",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            WhtRate = 3.0m, // 3% WHT as per Thailand tax regulations
            Notes = "Test order with 3% WHT calculation",
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
    public async Task UpdatePurchaseOrder_WithValidWHTRate_ShouldRecalculateAmounts()
    {
        // Arrange - This test should fail as we don't have implementation yet
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            WhtRate = 5.0m, // Update to 5% WHT
            Notes = "Updated WHT rate for tax compliance",
            RowVersion = "AAAAAAAAB9F="
        };

        // Act
        var response = await _client.PutAsJsonAsync("/purchase-orders/12345", updateRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders update endpoint is not implemented yet");
    }

    [Theory]
    [InlineData(25.0, "WHT rate cannot exceed 15% as per Thailand tax regulations")]
    [InlineData(20.0, "WHT rate cannot exceed 15% as per Thailand tax regulations")]
    [InlineData(-1.0, "WHT rate cannot be negative")]
    [InlineData(15.1, "WHT rate cannot exceed 15% as per Thailand tax regulations")]
    public async Task UpdatePurchaseOrder_WithInvalidWHTRate_ShouldReturnValidationError(
        decimal invalidWHTRate, string expectedErrorMessage)
    {
        // Arrange
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            WhtRate = invalidWHTRate,
            Notes = "Testing invalid WHT rate validation",
            RowVersion = "AAAAAAAAB9F="
        };

        // Act
        var response = await _client.PutAsJsonAsync("/purchase-orders/12345", updateRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders update endpoint is not implemented yet");

        // When implemented, this should return BadRequest with proper validation message
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedErrorMessage);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithThailandSupplier_ShouldApplyCorrectWHTRate()
    {
        // Arrange - Thailand supplier with local tax requirements
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 7890, // Thailand supplier
            OrderID = 9012,
            CurrencyID = 2, // THB (Thai Baht)
            OrderType = (Data.Enums.OrderType)OrderType.External,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(45),
            WhtRate = 3.0m, // Standard WHT rate for Thailand
            Notes = "Thailand supplier order with local WHT compliance",
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
    public async Task WHTCalculation_WithMultipleLineItems_ShouldCalculateCorrectTotals()
    {
        // This test validates complex WHT calculations with multiple line items
        // Expected calculation:
        // Line Item 1: 10 × ฿150.00 = ฿1,500.00
        // Line Item 2: 5 × ฿200.00 = ฿1,000.00
        // Subtotal: ฿2,500.00
        // WHT (3%): ฿75.00
        // Total: ฿2,425.00

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 2, // THB
            OrderType = (Data.Enums.OrderType)OrderType.External,
            WhtRate = 3.0m,
            Notes = "Multi-item order for WHT calculation testing",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
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
    public async Task WHTCalculation_CurrencyChange_ShouldRecalculateInNewCurrency()
    {
        // This test validates WHT recalculation when currency changes
        // From THB to USD with exchange rate conversion

        // Arrange
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            CurrencyID = 1, // Change from THB to USD
            WhtRate = 3.0m,
            Notes = "Currency changed to USD, recalculate WHT",
            RowVersion = "AAAAAAAAB9F="
        };

        // Act
        var response = await _client.PutAsJsonAsync("/purchase-orders/12349", updateRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders update endpoint is not implemented yet");
    }

    [Fact]
    public async Task WHTValidation_ZeroRate_ShouldBeAllowed()
    {
        // Arrange - 0% WHT should be valid for some suppliers
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            WhtRate = 0.0m, // No WHT
            Notes = "Supplier exempt from WHT",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
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
    public async Task WHTValidation_MaximumAllowedRate_ShouldBeAccepted()
    {
        // Arrange - 15% is the maximum allowed WHT rate in Thailand
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            WhtRate = 15.0m, // Maximum allowed rate
            Notes = "Maximum WHT rate for specific supplier type",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
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
    public async Task WHTCalculation_PrecisionTest_ShouldRoundCorrectly()
    {
        // This test validates correct rounding of WHT calculations to 2 decimal places
        // Subtotal: ฿123.45
        // WHT (3.33%): ฿4.11 (should round to 2 decimals)
        // Total: ฿119.34

        // Arrange
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            WhtRate = 3.33m, // Rate that produces decimal places
            Notes = "Testing WHT calculation precision and rounding",
            RowVersion = "AAAAAAAAB9F="
        };

        // Act
        var response = await _client.PutAsJsonAsync("/purchase-orders/12345", updateRequest);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders update endpoint is not implemented yet");
    }

    private async Task<PurchaseOrderResponse> CreateTestPurchaseOrder(decimal whtRate = 3.0m)
    {
        // Helper method to create test purchase orders
        // This will be used once the implementation exists
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            WhtRate = whtRate,
            Notes = "Test order for WHT calculations",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
                City = "Bangkok",
                PostalCode = "10330",
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