using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for GET /v1.0/purchase-orders/{id}/orderitems endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class GetOrderItemsContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public GetOrderItemsContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetOrderItems_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task GetOrderItems_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderItems_WithValidRequest_ShouldReturn200AndOrderItems()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var orderItems = JsonSerializer.Deserialize<List<OrderItemDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        orderItems.Should().NotBeNull();
        orderItems.Should().BeOfType<List<OrderItemDto>>();
    }

    [Fact]
    public async Task GetOrderItems_WithNonExistentPurchaseOrderId_ShouldReturn404()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var nonExistentId = 99999;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{nonExistentId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetOrderItems_WithInvalidId_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidId = "invalid";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{invalidId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrderItems_WithValidIdButNoItems_ShouldReturn200AndEmptyArray()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1; // Assuming this exists but has no items

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var orderItems = JsonSerializer.Deserialize<List<OrderItemDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        orderItems.Should().NotBeNull();
        orderItems.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderItems_WithOrderItemsSummaryQuery_ShouldReturn200AndSummary()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var summary = JsonSerializer.Deserialize<OrderItemsSummaryDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        summary.Should().NotBeNull();
        summary!.TotalItems.Should().BeGreaterThanOrEqualTo(0);
        summary.TotalQuantity.Should().BeGreaterThanOrEqualTo(0);
        summary.TotalValue.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOrderItems_WithPaginationParameters_ShouldReturn200AndPaginatedResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var page = 1;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems?page={page}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<OrderItemDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        paginatedResponse.Should().NotBeNull();
        paginatedResponse!.Data.Should().NotBeNull();
        paginatedResponse.Page.Should().Be(page);
        paginatedResponse.PageSize.Should().Be(pageSize);
        paginatedResponse.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOrderItems_WithInvalidPaginationParameters_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var invalidPage = -1;
        var invalidPageSize = 0;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems?page={invalidPage}&pageSize={invalidPageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError.Should().NotBeNull();
        validationError!.Errors.Should().Contain(e => e.Field.Contains("page", StringComparison.OrdinalIgnoreCase) || e.Field.Contains("pageSize", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetOrderItems_RoleBasedAccess_EmployeeRole_ShouldReturn200()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrderItems_RoleBasedAccess_InvalidRole_ShouldReturn403()
    {
        // Arrange
        var invalidRoleToken = TestJwtHelper.GenerateTestToken("test-user", "InvalidRole");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidRoleToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrderItems_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1/");
    }

    [Fact]
    public async Task GetOrderItems_WithCacheHeaders_ShouldIncludeCacheControl()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Order items should be cacheable for performance
        response.Headers.CacheControl?.MaxAge.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetOrderItems_ResponseFormat_ShouldIncludeRequiredFields()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/orderitems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var orderItems = JsonSerializer.Deserialize<List<OrderItemDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (orderItems?.Any() == true)
        {
            var firstItem = orderItems.First();
            firstItem.Id.Should().BeGreaterThan(0);
            firstItem.ProductName.Should().NotBeNullOrEmpty();
            firstItem.Quantity.Should().BeGreaterThan(0);
            firstItem.UnitPrice.Should().BeGreaterThanOrEqualTo(0);
            firstItem.TotalPrice.Should().BeGreaterThanOrEqualTo(0);
        }
    }

}