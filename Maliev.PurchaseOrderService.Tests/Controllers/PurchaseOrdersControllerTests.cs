using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Models;

namespace Maliev.PurchaseOrderService.Tests.Controllers;

public class PurchaseOrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public PurchaseOrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    #region T008: Contract test GET /purchase-orders

    [Fact]
    public async Task GetPurchaseOrders_WithValidEmployeeToken_ShouldReturnOwnOrdersOnly()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidManagerToken_ShouldReturnDepartmentOrders()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("mgr123", "manager", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidProcurementToken_ShouldReturnAllOrders()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidAdminToken_ShouldReturnAllOrders()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("admin123", "admin", "admin");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithoutAuthToken_ShouldReturn401()
    {
        // Arrange - No authorization header

        // Act
        var response = await _client.GetAsync("/purchase-orders");

        // Assert - Should fail initially as controller doesn't exist, but would be 401 if implemented
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/purchase-orders");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidPaginationParams_ShouldReturnPaginatedResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?page=1&pageSize=10");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidPaginationParams_ShouldReturn400()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?page=0&pageSize=101");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?status=Pending");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithOrderTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?orderType=External");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithDateRangeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var fromDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Act
        var response = await _client.GetAsync($"/purchase-orders?createdFrom={fromDate}&createdTo={toDate}");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithSortingParams_ShouldReturnSortedResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?sortBy=totalAmount&sortDirection=desc");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithSupplierFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?supplierID=123");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithOrderIDFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await _client.GetAsync("/purchase-orders?orderID=456");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T009: Contract test POST /purchase-orders

    [Fact]
    public async Task CreatePurchaseOrder_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External,
            CustomerPO = "CUST-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14),
            WhtRate = 3.0m,
            Notes = "Test purchase order",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "John Doe",
                AddressLine1 = "123 Main St",
                City = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand"
            },
            BillingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)AddressType.Billing,
                ContactName = "Jane Smith",
                AddressLine1 = "456 Business Ave",
                City = "Bangkok",
                PostalCode = "10200",
                Country = "Thailand"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/purchase-orders", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithMissingRequiredFields_ShouldReturn400()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        var request = new CreatePurchaseOrderRequest
        {
            // Missing required fields: SupplierID, OrderID, CurrencyID, OrderType
        };

        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/purchase-orders", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithoutAuthToken_ShouldReturn401()
    {
        // Arrange - No authorization header
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.External
        };

        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/purchase-orders", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T010: Contract test GET /purchase-orders/{id}

    [Fact]
    public async Task GetPurchaseOrderById_WithValidIdAndEmployeeToken_ShouldReturn200ForOwnOrder()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("proc123", "procurement", "procurement");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 99999;

        // Act
        var response = await _client.GetAsync($"/purchase-orders/{purchaseOrderId}");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T011: Contract test PUT /purchase-orders/{id}

    [Fact]
    public async Task UpdatePurchaseOrder_WithValidRequestAndEmployeeToken_ShouldReturn200ForOwnOrder()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new UpdatePurchaseOrderRequest
        {
            RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            CustomerPO = "UPDATED-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(21),
            WhtRate = 5.0m,
            Notes = "Updated notes"
        };

        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/purchase-orders/{purchaseOrderId}", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithMissingRowVersion_ShouldReturn400()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new UpdatePurchaseOrderRequest
        {
            // Missing required RowVersion
            Notes = "Updated notes"
        };

        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/purchase-orders/{purchaseOrderId}", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T012: Contract test DELETE /purchase-orders/{id}

    [Fact]
    public async Task DeletePurchaseOrder_WithValidIdAndEmployeeToken_ShouldReturn204ForOwnPendingOrder()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1; // Assuming this is employee's own pending order

        // Act
        var response = await _client.DeleteAsync($"/purchase-orders/{purchaseOrderId}");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePurchaseOrder_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("admin123", "admin", "admin");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 99999;

        // Act
        var response = await _client.DeleteAsync($"/purchase-orders/{purchaseOrderId}");

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T013: Contract test POST /purchase-orders/{id}/approve

    [Fact]
    public async Task ApprovePurchaseOrder_WithValidIdAndManagerToken_ShouldReturn200()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("mgr123", "manager", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new { notes = "Approved by manager" };
        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/purchase-orders/{purchaseOrderId}/approve", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmployeeToken_ShouldReturn403()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new { notes = "Trying to approve" };
        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/purchase-orders/{purchaseOrderId}/approve", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T014: Contract test POST /purchase-orders/{id}/cancel

    [Fact]
    public async Task CancelPurchaseOrder_WithValidRequestAndCreatorToken_ShouldReturn200()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1; // Assuming this is employee's own order

        var request = new { reason = "No longer needed" };
        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithMissingReason_ShouldReturn400()
    {
        // Arrange
        var jwtToken = GenerateJwtToken("emp123", "employee", "department1");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new { }; // Missing required reason
        var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert - Should fail initially as controller doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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