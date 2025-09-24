using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using Moq;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Common.Enumerations;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class SearchAndFilterOrdersTests : IntegrationTestBase
{
    public SearchAndFilterOrdersTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Search_Purchase_Orders_By_Purchase_Order_Number_Returns_Matching_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            SearchTerm = "PO-2024-001",
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(1);
        searchResults.Items.First().OrderNumber.Should().Be("PO-2024-001");
        searchResults.Pagination.TotalCount.Should().Be(1);
        searchResults.Pagination.Page.Should().Be(1);
        searchResults.Pagination.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Search_Purchase_Orders_By_Status_Returns_Filtered_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            Status = OrderStatus.Approved,
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();
        searchResults.Items.Should().OnlyContain(po => po.Status == OrderStatus.Approved);
    }

    [Fact]
    public async Task Search_Purchase_Orders_By_Type_Returns_Filtered_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            OrderType = OrderType.External,
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();
        searchResults.Items.Should().OnlyContain(po => po.OrderType == OrderType.External);
    }

    [Fact]
    public async Task Search_Purchase_Orders_By_Date_Range_Returns_Filtered_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow.AddDays(1);

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            CreatedFrom = fromDate,
            CreatedTo = toDate,
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();
        searchResults.Items.Should().OnlyContain(po =>
            po.CreatedAt >= fromDate && po.CreatedAt <= toDate);
    }

    [Fact]
    public async Task Search_Purchase_Orders_By_Supplier_Returns_Filtered_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var supplierId = await GetFirstSupplierIdFromTestData();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            SupplierId = supplierId,
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();
        searchResults.Items.Should().OnlyContain(po => po.SupplierID == supplierId);
    }

    [Fact]
    public async Task Search_Purchase_Orders_By_Customer_PO_Number_Returns_Matching_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            CustomerPoNumber = "CUST-PO-001",
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();
        searchResults.Items.Should().OnlyContain(po =>
            po.CustomerPO == "CUST-PO-001");
    }

    [Fact]
    public async Task Search_Purchase_Orders_With_Complex_Filters_Returns_Accurate_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            CurrencyCode = "THB",
            CreatedFrom = DateTime.UtcNow.AddDays(-30),
            CreatedTo = DateTime.UtcNow,
            Page = 1,
            PageSize = 5
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().OnlyContain(po =>
            po.OrderType == OrderType.Internal &&
            po.Status == OrderStatus.Pending &&
            po.CurrencyCode == "THB");
    }

    [Fact]
    public async Task Search_Purchase_Orders_With_Sorting_By_Created_Date_Descending_Returns_Ordered_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            SortBy = PurchaseOrderSortType.CreatedAtDesc,
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();

        // Verify descending order by created date
        for (int i = 0; i < searchResults.Items.Count - 1; i++)
        {
            searchResults.Items[i].CreatedAt.Should().BeOnOrAfter(searchResults.Items[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task Search_Purchase_Orders_With_Sorting_By_Total_Amount_Ascending_Returns_Ordered_Results()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            SortBy = PurchaseOrderSortType.TotalAmount,
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().NotBeEmpty();

        // Verify ascending order by total amount
        for (int i = 0; i < searchResults.Items.Count - 1; i++)
        {
            searchResults.Items[i].TotalAmount.Should().BeLessThanOrEqualTo(searchResults.Items[i + 1].TotalAmount);
        }
    }

    [Fact]
    public async Task Search_Purchase_Orders_With_Pagination_Returns_Correct_Page()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            Page = 2,
            PageSize = 2
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Pagination.Page.Should().Be(2);
        searchResults.Pagination.PageSize.Should().Be(2);
        searchResults.Items.Should().HaveCountLessThanOrEqualTo(2);
        searchResults.Pagination.TotalCount.Should().BeGreaterThan(0);
        searchResults.Pagination.TotalPages.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Search_Purchase_Orders_With_No_Results_Returns_Empty_List()
    {
        // Arrange
        await SeedTestData();
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            SearchTerm = "NON-EXISTENT-PO",
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().BeEmpty();
        searchResults.Pagination.TotalCount.Should().Be(0);
        searchResults.Pagination.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Search_Purchase_Orders_With_Invalid_Page_Size_Returns_Bad_Request()
    {
        // Arrange
        SetupEmployeeAuthentication();

        var searchRequest = new SearchPurchaseOrdersRequest
        {
            Page = 1,
            PageSize = 0 // Invalid page size
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_Purchase_Orders_Without_Authentication_Returns_Unauthorized()
    {
        // Arrange - No authentication token
        var searchRequest = new SearchPurchaseOrdersRequest
        {
            Page = 1,
            PageSize = 10
        };

        var queryString = BuildQueryString(searchRequest);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Purchase_Order_Statistics_Returns_Aggregated_Data()
    {
        // Arrange
        await SeedTestData();
        SetupManagerAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var stats = JsonSerializer.Deserialize<PurchaseOrderStatsDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        stats.Should().NotBeNull();
        stats!.TotalCount.Should().BeGreaterThan(0);
        stats.DraftCount.Should().BeGreaterThanOrEqualTo(0);
        stats.ApprovedCount.Should().BeGreaterThanOrEqualTo(0);
        stats.CanceledCount.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalValue.Should().BeGreaterThan(0);
    }

    private async Task SeedTestData()
    {
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var supplierId1 = 1234;
            var supplierId2 = 5678;

            // Clear any existing data to avoid conflicts
            dbContext.PurchaseOrders.RemoveRange(dbContext.PurchaseOrders);
            await dbContext.SaveChangesAsync();

            var purchaseOrders = new List<PurchaseOrder>
            {
                new()
                {
                    OrderNumber = "PO-2024-001",
                    OrderType = OrderType.Internal,
                    Status = OrderStatus.Pending,
                    SupplierID = supplierId1,
                    CurrencyCode = "THB",
                    TotalAmount = 1000.00m,
                    CreatedBy = "employee1@maliev.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    OrderNumber = "PO-2024-002",
                    OrderType = OrderType.External,
                    Status = OrderStatus.Approved,
                    SupplierID = supplierId2,
                    CurrencyCode = "USD",
                    TotalAmount = 2500.00m,
                    CustomerPO = "CUST-PO-001",
                    CreatedBy = "employee2@maliev.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    ApprovedBy = "manager@maliev.com",
                    ApprovedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    OrderNumber = "PO-2024-003",
                    OrderType = OrderType.Internal,
                    Status = OrderStatus.Cancelled,
                    SupplierID = supplierId1,
                    CurrencyCode = "THB",
                    TotalAmount = 750.00m,
                    CreatedBy = "employee1@maliev.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new()
                {
                    OrderNumber = "PO-2024-004",
                    OrderType = OrderType.Internal,
                    Status = OrderStatus.Approved,
                    SupplierID = supplierId2,
                    CurrencyCode = "THB",
                    TotalAmount = 3200.00m,
                    CreatedBy = "employee3@maliev.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ApprovedBy = "manager@maliev.com",
                    ApprovedAt = DateTime.UtcNow
                },
                new()
                {
                    OrderNumber = "PO-2024-005",
                    OrderType = OrderType.External,
                    Status = OrderStatus.Pending,
                    SupplierID = supplierId1,
                    CurrencyCode = "EUR",
                    TotalAmount = 1800.00m,
                    CustomerPO = "CUST-PO-002",
                    CreatedBy = "employee2@maliev.com",
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                }
            };

            dbContext.PurchaseOrders.AddRange(purchaseOrders);
            await dbContext.SaveChangesAsync();
        });
    }

    private async Task<int> GetFirstSupplierIdFromTestData()
    {
        return await ExecuteInDbContextAsync(async dbContext =>
        {
            var firstOrder = await dbContext.PurchaseOrders.FirstAsync();
            return firstOrder.SupplierID;
        });
    }


    private static string BuildQueryString(SearchPurchaseOrdersRequest request)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            queryParams.Add($"SearchTerm={HttpUtility.UrlEncode(request.SearchTerm)}");

        if (!string.IsNullOrEmpty(request.CustomerPoNumber))
            queryParams.Add($"CustomerPoNumber={HttpUtility.UrlEncode(request.CustomerPoNumber)}");

        if (request.OrderType.HasValue)
            queryParams.Add($"OrderType={request.OrderType}");

        if (request.Status.HasValue)
            queryParams.Add($"Status={request.Status}");

        if (request.SupplierId.HasValue)
            queryParams.Add($"SupplierId={request.SupplierId}");

        if (!string.IsNullOrEmpty(request.CurrencyCode))
            queryParams.Add($"CurrencyCode={request.CurrencyCode}");

        if (request.CreatedFrom.HasValue)
            queryParams.Add($"CreatedFrom={request.CreatedFrom:yyyy-MM-ddTHH:mm:ssZ}");

        if (request.CreatedTo.HasValue)
            queryParams.Add($"CreatedTo={request.CreatedTo:yyyy-MM-ddTHH:mm:ssZ}");

        queryParams.Add($"SortBy={request.SortBy}");

        queryParams.Add($"Page={request.Page}");
        queryParams.Add($"PageSize={request.PageSize}");

        return string.Join("&", queryParams);
    }

    private static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddSingleton(implementation);
    }
}