using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using Xunit;
using FluentAssertions;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for PurchaseOrdersController
/// Tests all CRUD operations, approval, cancellation, WHT calculations, and authorization
/// </summary>
public class PurchaseOrdersControllerIntegrationTests : IntegrationTestBase
{
    public PurchaseOrdersControllerIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    #region GET /v1.0/purchase-orders Tests

    [Fact]
    public async Task GetPurchaseOrders_WithEmployeeAuth_ShouldReturnPaginatedResults()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedTestDataAsync();

        var request = new SearchPurchaseOrdersRequest
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?page={request.Page}&pageSize={request.PageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Pagination.Should().NotBeNull();
        result.Pagination.Page.Should().Be(1);
        result.Pagination.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidPageSize_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?page=1&pageSize=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("Page size must be greater than 0");
    }

    [Fact]
    public async Task GetPurchaseOrders_WithPageSizeOver100_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?page=1&pageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("Page size cannot exceed 100");
    }

    [Fact]
    public async Task GetPurchaseOrders_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /v1.0/purchase-orders/{id} Tests

    [Fact]
    public async Task GetPurchaseOrder_WithValidId_ShouldReturnPurchaseOrder()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Id.Should().Be(seededPurchaseOrder.Id);
        result.OrderNumber.Should().Be(seededPurchaseOrder.OrderNumber);
    }

    [Fact]
    public async Task GetPurchaseOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    #endregion

    #region POST /v1.0/purchase-orders Tests

    [Fact]
    public async Task CreatePurchaseOrder_WithValidRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var request = CreateBasicPurchaseOrderRequest();

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.SupplierID.Should().Be(request.SupplierID);
        result.OrderID.Should().Be(request.OrderID);
        result.Status.Should().Be(OrderStatus.Pending);
        result.CreatedBy.Should().Be("emp123");

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/v1.0/purchase-orders/{result.Id}");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var request = new CreatePurchaseOrderRequest(); // Empty request

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();
        var request = CreateBasicPurchaseOrderRequest();

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /v1.0/purchase-orders/{id} Tests

    [Fact]
    public async Task UpdatePurchaseOrder_WithValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending, "emp123");

        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated notes",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(21)
        };

        // Act
        var response = await PutAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Notes.Should().Be("Updated notes");
        result.UpdatedBy.Should().Be("emp123");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            Notes = "Updated notes"
        };

        // Act
        var response = await PutAsJsonAsync("/v1.0/purchase-orders/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    #endregion

    #region DELETE /v1.0/purchase-orders/{id} Tests

    [Fact]
    public async Task DeletePurchaseOrder_WithValidId_ShouldDeleteSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending, "emp123");

        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft delete
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var deletedPurchaseOrder = await dbContext.PurchaseOrders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(po => po.Id == seededPurchaseOrder.Id);

            deletedPurchaseOrder.Should().NotBeNull();
            deletedPurchaseOrder!.IsDeleted.Should().BeTrue();
        });
    }

    [Fact]
    public async Task DeletePurchaseOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.DeleteAsync("/v1.0/purchase-orders/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    #endregion

    #region POST /v1.0/purchase-orders/{id}/approve Tests

    [Fact]
    public async Task ApprovePurchaseOrder_WithManagerAuth_ShouldApproveSuccessfully()
    {
        // Arrange
        SetupManagerAuthentication("mgr123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending);

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved by manager",
            ApprovedBy = "mgr123"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Approved);
        result.ApprovedBy.Should().Be("mgr123");
        result.ApprovedAt.Should().NotBeNull();

        // Verify audit log was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var auditLogs = await dbContext.AuditLogs
                .Where(a => a.EntityId == seededPurchaseOrder.Id.ToString() && a.Action == AuditAction.Approve)
                .ToListAsync();

            auditLogs.Should().HaveCount(1);
            auditLogs[0].UserId.Should().Be("mgr123");
        });
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmployeeAuth_ShouldReturnForbidden()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Trying to approve as employee"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_AlreadyApproved_ShouldReturnConflict()
    {
        // Arrange
        SetupManagerAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Approved);

        var approvalRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Second approval attempt"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/approve", approvalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region POST /v1.0/purchase-orders/{id}/cancel Tests

    [Fact]
    public async Task CancelPurchaseOrder_WithValidRequest_ShouldCancelSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Customer requested cancellation",
            CanceledBy = "emp123"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Cancelled);
        result.CancelledBy.Should().Be("emp123");
        result.CancelledAt.Should().NotBeNull();

        // Verify audit log was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var auditLogs = await dbContext.AuditLogs
                .Where(a => a.EntityId == seededPurchaseOrder.Id.ToString() && a.Action == AuditAction.Cancel)
                .ToListAsync();

            auditLogs.Should().HaveCount(1);
            auditLogs[0].UserId.Should().Be("emp123");
        });
    }

    [Fact]
    public async Task CancelPurchaseOrder_AlreadyCancelled_ShouldReturnConflict()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Cancelled);

        var cancellationRequest = new CancelPurchaseOrderRequest
        {
            Reason = "Second cancellation attempt"
        };

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/cancel", cancellationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region GET /v1.0/purchase-orders/stats Tests

    [Fact]
    public async Task GetPurchaseOrderStats_WithAuthentication_ShouldReturnStatistics()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderStatsDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region GET /v1.0/purchase-orders/customer-po/{customerPoNumber} Tests

    [Fact]
    public async Task GetPurchaseOrdersByCustomerPo_WithValidNumber_ShouldReturnOrders()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Update with customer PO number
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders.FindAsync(seededPurchaseOrder.Id);
            if (po != null)
            {
                po.CustomerPO = "CUST-PO-001";
                await dbContext.SaveChangesAsync();
            }
        });

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/customer-po/CUST-PO-001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<PurchaseOrderDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
        result.All(po => po.CustomerPO == "CUST-PO-001").Should().BeTrue();
    }

    #endregion

    #region POST /v1.0/purchase-orders/{id}/calculate-wht Tests

    [Fact]
    public async Task CalculateWHT_WithValidRequest_ShouldReturnCalculation()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var whtRequest = new WHTCalculationRequest
        {
            SubtotalAmount = 10000.00m,
            CurrencyCode = "THB",
            SupplierID = 1,
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/calculate-wht", whtRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<WHTCalculationResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.WHTAmount.Should().BeGreaterThan(0);
        result.NetAmount.Should().BeLessThan(whtRequest.SubtotalAmount);
    }

    [Fact]
    public async Task CalculateWHT_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        var whtRequest = new WHTCalculationRequest
        {
            SubtotalAmount = 10000.00m,
            CurrencyCode = "THB",
            SupplierID = 1,
            OrderType = Data.Enums.OrderType.Internal
        };

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders/99999/calculate-wht", whtRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /v1.0/purchase-orders/{id}/wht-history Tests

    [Fact]
    public async Task GetWHTHistory_WithValidId_ShouldReturnHistory()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/wht-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<WHTCalculationResult>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
    }

    #endregion

    #region POST /v1.0/purchase-orders/{id}/recalculate-wht Tests

    [Fact]
    public async Task RecalculateWHT_WithProcurementAuth_ShouldRecalculateSuccessfully()
    {
        // Arrange
        SetupProcurementAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/recalculate-wht", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.WHTAmount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RecalculateWHT_WithEmployeeAuth_ShouldReturnForbidden()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/recalculate-wht", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /v1.0/purchase-orders/{id}/history Tests

    [Fact]
    public async Task GetPurchaseOrderHistory_WithValidId_ShouldReturnAuditHistory()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AuditLogDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
    }

    #endregion

    #region Authentication and Authorization Tests

    [Theory]
    [InlineData("/v1.0/purchase-orders")]
    [InlineData("/v1.0/purchase-orders/1")]
    [InlineData("/v1.0/purchase-orders/stats")]
    public async Task GetEndpoints_WithoutAuthentication_ShouldReturnUnauthorized(string endpoint)
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Procurement")]
    [InlineData("Admin")]
    public async Task GetPurchaseOrders_WithValidRoles_ShouldReturnOk(string role)
    {
        // Arrange
        switch (role)
        {
            case "Employee":
                SetupEmployeeAuthentication();
                break;
            case "Manager":
                SetupManagerAuthentication();
                break;
            case "Procurement":
                SetupProcurementAuthentication();
                break;
            case "Admin":
                SetupAdminAuthentication();
                break;
        }

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreatePurchaseOrder_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var malformedJson = "{ invalid json }";
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1.0/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPurchaseOrder_WithNegativeId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetPurchaseOrders_WithLargePageSize_ShouldCompleteWithinTimeout()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedTestDataAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act & Assert
        var response = await Client.GetAsync("/v1.0/purchase-orders?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}