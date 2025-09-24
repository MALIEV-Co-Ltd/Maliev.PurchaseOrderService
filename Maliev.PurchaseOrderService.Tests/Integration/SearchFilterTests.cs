using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 4: Search and filter operations
/// </summary>
public class SearchFilterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SearchFilterTests(WebApplicationFactory<Program> factory)
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
                    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PurchaseOrderDbContext")
                        ?? "Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=postgres;";
                    options.UseNpgsql(connectionString);
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                });
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrders = new List<PurchaseOrder>
        {
            new PurchaseOrder
            {
                OrderNumber = "PO-2025-SEARCH-001",
                SupplierID = 1,
                OrderID = 1,
                CurrencyID = 1,
                SupplierName = "Alpha Supplier",
                CurrencyCode = "THB",
                CurrencySymbol = "฿",
                Currency = "THB",
                OrderDate = DateTime.UtcNow.AddDays(-10),
                Status = OrderStatus.Pending,
                OrderType = OrderType.Internal,
                SubtotalAmount = 5000m,
                TotalAmount = 5000m,
                CreatedBy = "employee1",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new PurchaseOrder
            {
                OrderNumber = "PO-2025-SEARCH-002",
                SupplierID = 2,
                OrderID = 2,
                CurrencyID = 2,
                SupplierName = "Beta Supplier",
                CurrencyCode = "USD",
                CurrencySymbol = "$",
                Currency = "USD",
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Status = OrderStatus.Approved,
                OrderType = OrderType.External,
                SubtotalAmount = 15000m,
                TotalAmount = 14550m,
                WHTRate = 3m,
                WHTAmount = 450m,
                CreatedBy = "employee2",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ApprovedBy = "manager1",
                ApprovedAt = DateTime.UtcNow.AddDays(-4)
            },
            new PurchaseOrder
            {
                OrderNumber = "PO-2025-SEARCH-003",
                SupplierID = 1,
                OrderID = 3,
                CurrencyID = 1,
                SupplierName = "Alpha Supplier",
                CurrencyCode = "THB",
                CurrencySymbol = "฿",
                Currency = "THB",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                Status = OrderStatus.Cancelled,
                OrderType = OrderType.Internal,
                SubtotalAmount = 8000m,
                TotalAmount = 8000m,
                CreatedBy = "employee1",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new PurchaseOrder
            {
                OrderNumber = "PO-2025-SEARCH-004",
                SupplierID = 3,
                OrderID = 4,
                CurrencyID = 3,
                SupplierName = "Gamma Supplier",
                CurrencyCode = "EUR",
                CurrencySymbol = "€",
                Currency = "EUR",
                OrderDate = DateTime.UtcNow.AddDays(-1),
                Status = OrderStatus.Pending,
                OrderType = OrderType.External,
                SubtotalAmount = 20000m,
                TotalAmount = 19400m,
                WHTRate = 3m,
                WHTAmount = 600m,
                CreatedBy = "employee3",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        context.PurchaseOrders.AddRange(purchaseOrders);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithoutFilters_ShouldReturnAllOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByStatus_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?status=Approved");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.ToArray()[0].Status.Should().Be(OrderStatus.Approved);
        result.Data.ToArray()[0].OrderNumber.Should().Be("PO-2025-SEARCH-002");
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByOrderType_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?orderType=External");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Data.Should().AllSatisfy(po => po.OrderType.Should().Be(OrderType.External));
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterBySupplier_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?supplierName=Alpha");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Data.Should().AllSatisfy(po => po.SupplierName.Should().Contain("Alpha"));
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByDateRange_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();
        var fromDate = DateTime.UtcNow.AddDays(-6).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/purchaseorders/v1.0/purchase-orders?fromDate={fromDate}&toDate={toDate}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCountGreaterThan(0);
        result.Data.Should().AllSatisfy(po =>
        {
            po.OrderDate.Should().BeOnOrAfter(DateTime.Parse(fromDate));
            po.OrderDate.Should().BeOnOrBefore(DateTime.Parse(toDate).AddDays(1));
        });
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByAmountRange_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?minAmount=10000&maxAmount=20000");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCountGreaterThan(0);
        result.Data.Should().AllSatisfy(po =>
        {
            po.TotalAmount.Should().BeGreaterThanOrEqualTo(10000m);
            po.TotalAmount.Should().BeLessThanOrEqualTo(20000m);
        });
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithSorting_ShouldReturnOrderedResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Sort by total amount descending
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?sortBy=TotalAmountDesc");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(4);

        var amounts = result.Data.Select(po => po.TotalAmount).ToList();
        amounts.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Get first page with 2 items per page
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithComplexFilter_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Multiple filters: Internal orders, Pending status, amount > 4000
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?orderType=Internal&status=Pending&minAmount=4000");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.ToArray()[0].OrderType.Should().Be(OrderType.Internal);
        result.Data.ToArray()[0].Status.Should().Be(OrderStatus.Pending);
        result.Data.ToArray()[0].TotalAmount.Should().BeGreaterThan(4000m);
        result.Data.ToArray()[0].OrderNumber.Should().Be("PO-2025-SEARCH-001");
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithTextSearch_ShouldReturnMatchingOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?searchText=PO-2025-SEARCH-002");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.ToArray()[0].OrderNumber.Should().Be("PO-2025-SEARCH-002");
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithInvalidParameters_ShouldReturnValidationError()
    {
        // Act - Invalid page size
        var response = await _client.GetAsync("/purchaseorders/v1.0/purchase-orders?pageSize=0");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Errors.Should().Contain(e => e.Field == "PageSize");
    }
}