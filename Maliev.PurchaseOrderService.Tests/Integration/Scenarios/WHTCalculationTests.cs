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
using Maliev.PurchaseOrderService.Data.Enums;
using System.Net;
using System.Net.Mime;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class WHTCalculationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ISupplierServiceClient> _mockSupplierService;
    private readonly Mock<IOrderServiceClient> _mockOrderService;
    private readonly Mock<ICurrencyServiceClient> _mockCurrencyService;
    private readonly Mock<IWHTCalculationService> _mockWHTService;

    public WHTCalculationTests(WebApplicationFactory<Program> factory)
    {
        _mockSupplierService = new Mock<ISupplierServiceClient>();
        _mockOrderService = new Mock<IOrderServiceClient>();
        _mockCurrencyService = new Mock<ICurrencyServiceClient>();
        _mockWHTService = new Mock<IWHTCalculationService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing
                services.AddDbContext<PurchaseOrderContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_WHTCalculation");
                });

                // Replace external service clients with mocks
                services.AddSingleton(_mockSupplierService.Object);
                services.AddSingleton(_mockOrderService.Object);
                services.AddSingleton(_mockCurrencyService.Object);
                services.AddSingleton(_mockWHTService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Calculate_WHT_For_Thailand_Supplier_Returns_Correct_Amount()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupThailandSupplierMock();

        var purchaseOrderId = await CreateTestPurchaseOrder();
        var calculationRequest = new WHTCalculationRequest
        {
            PurchaseOrderId = 1001,
            SupplierID = 1234,
            CurrencyCode = "THB",
            OrderType = OrderType.Internal,
            TotalAmount = 10000.00m,
            WHTRate = 0.03m // 3% WHT for Thailand
        };

        _mockWHTService
            .Setup(x => x.CalculateWHTAsync(
                It.IsAny<SupplierDto>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WHTCalculationResult
            {
                WHTAmount = 300.00m, // 3% of 10,000
                NetAmount = 9700.00m, // 10,000 - 300
                WHTRate = 0.03m,
                IsApplicable = true,
                TaxRegulation = "Thailand Revenue Code Section 3"
            });

        var json = JsonSerializer.Serialize(calculationRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/calculate-wht", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<WHTCalculationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.WHTAmount.Should().Be(300.00m);
        result.NetAmount.Should().Be(9700.00m);
        result.WHTRate.Should().Be(0.03m);
        result.IsApplicable.Should().BeTrue();
        result.TaxRegulation.Should().Be("Thailand Revenue Code Section 3");

        // Verify service call
        _mockWHTService.Verify(x => x.CalculateWHTAsync(
            It.IsAny<SupplierDto>(),
            It.Is<decimal>(amount => amount == 10000.00m),
            It.Is<string>(currency => currency == "THB"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calculate_WHT_For_Foreign_Supplier_Returns_Zero_WHT()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupForeignSupplierMock();

        var purchaseOrderId = await CreateTestPurchaseOrder();
        var calculationRequest = new WHTCalculationRequest
        {
            PurchaseOrderId = 1002,
            SupplierID = 1234,
            CurrencyCode = "USD",
            OrderType = OrderType.External,
            TotalAmount = 10000.00m,
            WHTRate = 0.00m // No WHT for foreign suppliers
        };

        _mockWHTService
            .Setup(x => x.CalculateWHTAsync(
                It.IsAny<SupplierDto>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WHTCalculationResult
            {
                WHTAmount = 0.00m,
                NetAmount = 10000.00m,
                WHTRate = 0.00m,
                IsApplicable = false,
                TaxRegulation = "Not applicable for foreign suppliers"
            });

        var json = JsonSerializer.Serialize(calculationRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/calculate-wht", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<WHTCalculationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.WHTAmount.Should().Be(0.00m);
        result.NetAmount.Should().Be(10000.00m);
        result.WHTRate.Should().Be(0.00m);
        result.IsApplicable.Should().BeFalse();
    }

    [Fact]
    public async Task Calculate_WHT_For_Service_Type_Purchase_Order_Uses_Higher_Rate()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupThailandSupplierMock();

        var purchaseOrderId = await CreateTestPurchaseOrder();
        var calculationRequest = new WHTCalculationRequest
        {
            PurchaseOrderId = 1003,
            SupplierID = 1234,
            CurrencyCode = "THB",
            OrderType = OrderType.Internal,
            TotalAmount = 10000.00m,
            WHTRate = 0.05m, // 5% WHT for services
            ServiceType = "Professional Services"
        };

        _mockWHTService
            .Setup(x => x.CalculateWHTAsync(
                It.IsAny<SupplierDto>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WHTCalculationResult
            {
                WHTAmount = 500.00m, // 5% of 10,000
                NetAmount = 9500.00m, // 10,000 - 500
                WHTRate = 0.05m,
                IsApplicable = true,
                TaxRegulation = "Thailand Revenue Code Section 40(4)(a)"
            });

        var json = JsonSerializer.Serialize(calculationRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/calculate-wht", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<WHTCalculationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.WHTAmount.Should().Be(500.00m);
        result.NetAmount.Should().Be(9500.00m);
        result.WHTRate.Should().Be(0.05m);
        result.TaxRegulation.Should().Be("Thailand Revenue Code Section 40(4)(a)");
    }

    [Fact]
    public async Task Apply_WHT_To_Purchase_Order_Updates_Amounts_Correctly()
    {
        // Arrange
        SetupManagerAuthentication();
        SetupThailandSupplierMock();

        var purchaseOrderId = await CreateTestPurchaseOrder();
        var applyWHTRequest = new UpdatePurchaseOrderRequest
        {
            RowVersion = "1",
            WhtRate = 0.03m,
            Notes = "WHT Applied: 300.00 THB at 3% rate - Thailand Revenue Code Section 3"
        };

        var json = JsonSerializer.Serialize(applyWHTRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PutAsync($"/api/purchaseorders/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify purchase order was updated in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        var updatedOrder = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(po => po.OrderNumber.Contains("Test"));

        updatedOrder.Should().NotBeNull();
        updatedOrder!.Notes.Should().Contain("WHT Applied");
    }

    [Fact]
    public async Task Calculate_WHT_For_Nonexistent_Purchase_Order_Returns_NotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        var nonexistentOrderId = 9999;
        var calculationRequest = new WHTCalculationRequest
        {
            PurchaseOrderId = nonexistentOrderId,
            SupplierID = 1234,
            CurrencyCode = "THB",
            OrderType = OrderType.Internal,
            TotalAmount = 10000.00m,
            WHTRate = 0.03m
        };

        var json = JsonSerializer.Serialize(calculationRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{nonexistentOrderId}/calculate-wht", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Calculate_WHT_With_Invalid_Rate_Returns_BadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupThailandSupplierMock();

        var purchaseOrderId = await CreateTestPurchaseOrder();
        var calculationRequest = new WHTCalculationRequest
        {
            PurchaseOrderId = 1004,
            SupplierID = 1234,
            CurrencyCode = "THB",
            OrderType = OrderType.Internal,
            TotalAmount = 10000.00m,
            WHTRate = 1.5m // Invalid rate > 100%
        };

        var json = JsonSerializer.Serialize(calculationRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/calculate-wht", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Invalid WHT rate");
    }

    [Fact]
    public async Task Employee_Cannot_Apply_WHT_Without_Manager_Authorization()
    {
        // Arrange
        SetupEmployeeAuthentication(); // Employee, not Manager
        SetupThailandSupplierMock();

        var purchaseOrderId = await CreateTestPurchaseOrder();
        var applyWHTRequest = new UpdatePurchaseOrderRequest
        {
            RowVersion = "1",
            WhtRate = 0.03m,
            Notes = "WHT Applied: 300.00 THB at 3% rate - Thailand Revenue Code Section 3"
        };

        var json = JsonSerializer.Serialize(applyWHTRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await _client.PutAsync($"/api/purchaseorders/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_WHT_History_For_Purchase_Order_Returns_All_Calculations()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var purchaseOrderId = await CreateTestPurchaseOrder();

        // Act
        var response = await _client.GetAsync($"/api/purchaseorders/{purchaseOrderId}/wht-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var history = JsonSerializer.Deserialize<List<WHTCalculationResult>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        history.Should().NotBeNull();
        // Will be empty initially, but endpoint should work
        history!.Should().BeOfType<List<WHTCalculationResult>>();
    }

    private async Task<int> CreateTestPurchaseOrder()
    {
        SetupExternalServiceMocks();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            CurrencyID = 1,
            Notes = "Test order for WHT calculation",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 10000.00m,
                    Notes = "Test item"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await _client.PostAsync("/api/purchaseorders", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<PurchaseOrderResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return createdOrder!.Id;
    }

    private void SetupEmployeeAuthentication()
    {
        var token = "Bearer mock-employee-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupManagerAuthentication()
    {
        var token = "Bearer mock-manager-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupExternalServiceMocks()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto { Id = Guid.NewGuid(), Name = "Test Supplier" });

        _mockSupplierService
            .Setup(x => x.GetSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Supplier",
                IsActive = true,
                IsThaiResident = true
            });

        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrencyDto { Code = "THB", Name = "Thai Baht", Symbol = "฿" });

        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    Id = 1,
                    Quantity = 1,
                    UnitPrice = 10000.00m,
                    TotalPrice = 10000.00m
                }
            });
    }

    private void SetupThailandSupplierMock()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto { Id = Guid.NewGuid(), Name = "Thailand Supplier" });

        _mockSupplierService
            .Setup(x => x.GetSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto
            {
                Id = Guid.NewGuid(),
                Name = "Thailand Supplier",
                IsActive = true,
                IsThaiResident = true
            });
    }

    private void SetupForeignSupplierMock()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto { Id = Guid.NewGuid(), Name = "Foreign Supplier" });

        _mockSupplierService
            .Setup(x => x.GetSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto
            {
                Id = Guid.NewGuid(),
                Name = "Foreign Supplier",
                IsActive = true,
                IsThaiResident = false
            });
    }
}