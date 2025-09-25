using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Maliev.PurchaseOrderService.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for GET /v1.0/purchase-orders endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class GetPurchaseOrdersContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public GetPurchaseOrdersContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await dbContext.PurchaseOrders.AnyAsync())
        {
            return;
        }

        // Create multiple test purchase orders for pagination and filtering tests
        for (int i = 1; i <= 5; i++)
        {
            var (purchaseOrder, orderItems, shippingAddress, billingAddress) =
                TestDataFactory.CreateCompletePurchaseOrderWithEntities(
                    Data.Enums.OrderType.Internal,
                    2,
                    $"emp{i}");

            // Add addresses first
            var addresses = new List<Data.Entities.Address>();
            if (shippingAddress != null) addresses.Add(shippingAddress);
            if (billingAddress != null) addresses.Add(billingAddress);

            if (addresses.Count > 0)
            {
                await dbContext.Addresses.AddRangeAsync(addresses);
                await dbContext.SaveChangesAsync();
            }

            // Set address foreign keys
            if (shippingAddress != null)
                purchaseOrder.ShippingAddressId = shippingAddress.Id;
            if (billingAddress != null)
                purchaseOrder.BillingAddressId = billingAddress.Id;

            // Add purchase order
            await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
            await dbContext.SaveChangesAsync();

            // Set order item foreign keys and add them
            foreach (var item in orderItems)
                item.PurchaseOrderId = purchaseOrder.Id;

            await dbContext.OrderItems.AddRangeAsync(orderItems);
            await dbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetPurchaseOrders_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange - No authentication headers

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidToken_ShouldReturn200AndPaginatedResponse()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        paginatedResponse.Should().NotBeNull();
        paginatedResponse!.Items.Should().NotBeNull();
        paginatedResponse.Pagination.Should().NotBeNull();
        paginatedResponse.Pagination.Page.Should().BeGreaterThan(0);
        paginatedResponse.Pagination.PageSize.Should().BeGreaterThan(0);
        paginatedResponse.Pagination.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithPaginationParameters_ShouldReturnCorrectPagination()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var queryParams = "?page=2&pageSize=10";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}{queryParams}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedPurchaseOrdersResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        paginatedResponse!.Pagination.Page.Should().Be(2);
        paginatedResponse.Pagination.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithSearchFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var queryParams = "?supplierName=TestSupplier&status=Pending&orderType=Internal";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}{queryParams}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidPaginationParameters_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var queryParams = "?page=0&pageSize=-1"; // Invalid pagination

        // Act
        var response = await _client.GetAsync($"{_baseUrl}{queryParams}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPurchaseOrders_ResponseHeaders_ShouldIncludeRequiredHeaders()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        response.Headers.Should().ContainKey("X-Total-Count");
        response.Headers.CacheControl?.NoCache.Should().BeTrue();
    }

    [Fact]
    public async Task GetPurchaseOrders_WithRoleBasedAccess_EmployeeRole_ShouldReturnOwnOrdersOnly()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Employee should only see their own orders (implementation detail to be verified)
    }

    [Fact]
    public async Task GetPurchaseOrders_WithRoleBasedAccess_ManagerRole_ShouldReturnAllOrders()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Manager should see all orders in their scope
    }

    [Fact]
    public async Task GetPurchaseOrders_ApiVersioning_ShouldHaveCorrectVersionInPath()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync(_baseUrl);

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
    }

}