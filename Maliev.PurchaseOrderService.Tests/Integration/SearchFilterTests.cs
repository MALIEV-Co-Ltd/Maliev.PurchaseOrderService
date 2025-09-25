using System.Text.Json;
using Xunit;
using FluentAssertions;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Common.Enumerations;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration test Scenario 4: Search and filter operations
/// </summary>
public class SearchFilterTests : IntegrationTestBase
{
    public SearchFilterTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    private async Task<List<PurchaseOrder>> SeedSearchTestDataAsync()
    {
        var purchaseOrders = new List<PurchaseOrder>();

        // Create test POs with proper addresses using the base class method
        var po1 = await SeedPurchaseOrderAsync(
            orderType: OrderType.Internal,
            status: OrderStatus.Pending,
            createdBy: "employee1"
        );
        po1 = await UpdatePurchaseOrderInDbAsync(po1.Id, po =>
        {
            po.OrderNumber = "PO-2025-SEARCH-001";
            po.SupplierName = "Alpha Supplier";
            po.CurrencyCode = "THB";
            po.SubtotalAmount = 5000m;
            po.TotalAmount = 5000m;
            po.CreatedAt = DateTime.UtcNow.AddDays(-10);
        });
        purchaseOrders.Add(po1);

        var po2 = await SeedPurchaseOrderAsync(
            orderType: OrderType.External,
            status: OrderStatus.Approved,
            createdBy: "employee2"
        );
        po2 = await UpdatePurchaseOrderInDbAsync(po2.Id, po =>
        {
            po.OrderNumber = "PO-2025-SEARCH-002";
            po.SupplierName = "Beta Supplier";
            po.CurrencyCode = "USD";
            po.SubtotalAmount = 15000m;
            po.TotalAmount = 14550m;
            po.WHTRate = 3m;
            po.WHTAmount = 450m;
            po.CreatedAt = DateTime.UtcNow.AddDays(-5);
            po.ApprovedBy = "manager1";
            po.ApprovedAt = DateTime.UtcNow.AddDays(-4);
        });
        purchaseOrders.Add(po2);

        var po3 = await SeedPurchaseOrderAsync(
            orderType: OrderType.Internal,
            status: OrderStatus.Cancelled,
            createdBy: "employee1"
        );
        po3 = await UpdatePurchaseOrderInDbAsync(po3.Id, po =>
        {
            po.OrderNumber = "PO-2025-SEARCH-003";
            po.SupplierName = "Alpha Supplier";
            po.CurrencyCode = "THB";
            po.SubtotalAmount = 8000m;
            po.TotalAmount = 8000m;
            po.CreatedAt = DateTime.UtcNow.AddDays(-2);
        });
        purchaseOrders.Add(po3);

        var po4 = await SeedPurchaseOrderAsync(
            orderType: OrderType.External,
            status: OrderStatus.Pending,
            createdBy: "employee3"
        );
        po4 = await UpdatePurchaseOrderInDbAsync(po4.Id, po =>
        {
            po.OrderNumber = "PO-2025-SEARCH-004";
            po.SupplierName = "Gamma Supplier";
            po.CurrencyCode = "EUR";
            po.SubtotalAmount = 20000m;
            po.TotalAmount = 19400m;
            po.WHTRate = 3m;
            po.WHTAmount = 600m;
            po.CreatedAt = DateTime.UtcNow.AddDays(-1);
        });
        purchaseOrders.Add(po4);

        return purchaseOrders;
    }

    private async Task<PurchaseOrder> UpdatePurchaseOrderInDbAsync(int purchaseOrderId, Action<PurchaseOrder> updateAction)
    {
        return await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders.FindAsync(purchaseOrderId);
            if (po != null)
            {
                updateAction(po);
                await dbContext.SaveChangesAsync();
            }
            return po!;
        });
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithoutFilters_ShouldReturnAllOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByStatus_ShouldReturnMatchingOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?status=Approved");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.ToArray()[0].Status.Should().Be(OrderStatus.Approved);
        result.Data.ToArray()[0].OrderNumber.Should().Be("PO-2025-SEARCH-002");
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByOrderType_ShouldReturnMatchingOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?orderType=External");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Data.Should().AllSatisfy(po => po.OrderType.Should().Be(OrderType.External));
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterBySupplier_ShouldReturnMatchingOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?supplierName=Alpha");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Data.Should().AllSatisfy(po => po.SupplierName.Should().Contain("Alpha"));
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByDateRange_ShouldReturnMatchingOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();
        var fromDate = DateTime.UtcNow.AddDays(-6).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?createdFrom={fromDate}&createdTo={toDate}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCountGreaterThan(0);
        result.Data.Should().AllSatisfy(po =>
        {
            po.CreatedAt.Should().BeOnOrAfter(DateTime.Parse(fromDate));
            po.CreatedAt.Should().BeOnOrBefore(DateTime.Parse(toDate).AddDays(1));
        });
    }

    [Fact]
    public async Task SearchPurchaseOrders_FilterByAmountRange_ShouldReturnMatchingOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?minAmount=10000&maxAmount=20000");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

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
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act - Sort by total amount descending
        var response = await Client.GetAsync("/v1.0/purchase-orders?sortBy=TotalAmountDesc");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(4);

        var amounts = result.Data.Select(po => po.TotalAmount).ToList();
        amounts.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act - Get first page with 2 items per page
        var response = await Client.GetAsync("/v1.0/purchase-orders?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

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
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act - Multiple filters: Internal orders, Pending status, amount > 4000
        var response = await Client.GetAsync("/v1.0/purchase-orders?orderType=Internal&status=Pending&minAmount=4000");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

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
        SetupEmployeeAuthentication();
        await SeedSearchTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?searchText=PO-2025-SEARCH-002");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.ToArray()[0].OrderNumber.Should().Be("PO-2025-SEARCH-002");
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithInvalidParameters_ShouldReturnValidationError()
    {
        // Act - Invalid page size
        var response = await Client.GetAsync("/v1.0/purchase-orders?pageSize=0");

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