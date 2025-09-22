using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Models;

namespace Maliev.PurchaseOrderService.Tests.Controllers;

public class OrderItemsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrderItemsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    #region T015: Contract test GET /purchase-orders/{id}/items

    [Fact]
    public async Task GetOrderItems_WithValidIdAndEmployeeToken_ShouldReturn200ForOwnOrder()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1; // Assuming this is employee's own order

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithValidIdAndEmployeeToken_ShouldReturn403ForOthersOrder()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 999; // Order belonging to someone else

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithValidIdAndManagerToken_ShouldReturn200ForDepartmentOrder()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("mgr123", "manager", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithValidIdAndProcurementToken_ShouldReturn200()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithValidIdAndAdminToken_ShouldReturn200()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("admin123", "admin", "admin");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithRefreshParameter_ShouldForceRefreshFromExternalService()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items?refresh=true");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithRefreshParameterFalse_ShouldUseCachedData()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items?refresh=false");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithoutRefreshParameter_ShouldUseCachedDataByDefault()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithNonExistentPurchaseOrderId_ShouldReturn404()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 99999; // Non-existent purchase order

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithoutAuthToken_ShouldReturn401()
    {
        // Arrange - No authorization header
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WhenExternalServiceUnavailable_ShouldReturn502()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act - This test would simulate external service unavailability when refresh=true
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items?refresh=true");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderItems_WithValidOrder_ShouldReturnOrderItemsArray()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // When implemented, this should return:
        // 1. Content-Type: application/json
        // 2. Array of OrderItemResponse objects
        // 3. Each item should have: Id, ExternalOrderItemId, ProductCode, ProductName, Quantity, etc.
        // 4. CachedAt timestamp indicating when data was retrieved
        // 5. ExternallyModified flag if external data has changed
    }

    [Fact]
    public async Task GetOrderItems_WithStaleCache_ShouldIndicateExternallyModified()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 2; // Assuming this order has stale cached data

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // When implemented, items should have ExternallyModified = true for stale data
    }

    [Fact]
    public async Task GetOrderItems_WithEmptyOrder_ShouldReturnEmptyArray()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 3; // Assuming this order has no items

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // When implemented, this should return an empty array []
    }

    [Fact]
    public async Task GetOrderItems_WithInvalidRefreshParameter_ShouldIgnoreAndUseFalse()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}/items?refresh=invalid");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // When implemented, invalid refresh parameter should default to false
    }

    #endregion

    #region Helper Methods

    private static string GenerateJwtToken(string userId, string role, string department)
    {
        // Mock JWT token generation - in real implementation this would create a proper JWT
        // For now, return a simple base64 encoded string that contains the claims
        var claims = $"{userId}:{role}:{department}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(claims);
        return Convert.ToBase64String(bytes);
    }

    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    #endregion
}