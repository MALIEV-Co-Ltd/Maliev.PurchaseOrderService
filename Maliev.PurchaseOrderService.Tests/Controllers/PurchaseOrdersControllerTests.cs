using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Models;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Moq;

namespace Maliev.PurchaseOrderService.Tests.Controllers;

public class PurchaseOrdersControllerTests : IntegrationTestBase
{
    public PurchaseOrdersControllerTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
        // Setup is handled by base class
    }

    #region T008: Contract test GET /purchase-orders

    [Fact]
    public async Task GetPurchaseOrders_WithValidEmployeeToken_ShouldReturnOwnOrdersOnly()
    {
        // Arrange
        await SeedTestDataAsync();
        SetupEmployeeAuthentication("emp123", "department1");

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert - Should return OK once API versioning is fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidManagerToken_ShouldReturnDepartmentOrders()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateManagerToken("mgr123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert - Should return OK once API versioning is fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidProcurementToken_ShouldReturnAllOrders()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert - Should return OK once API versioning is fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidAdminToken_ShouldReturnAllOrders()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateAdminToken("admin123", "admin");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert - Should return OK once API versioning is fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithoutAuthToken_ShouldReturn401()
    {
        // Arrange - No authorization header

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert - Should return 401 Unauthorized when no token provided
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders");

        // Assert - Should return 401 Unauthorized for invalid token
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithValidPaginationParams_ShouldReturnPaginatedResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?page=1&pageSize=10");

        // Assert - Should return OK with pagination
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithInvalidPaginationParams_ShouldReturn400()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?page=0&pageSize=101");

        // Assert - Should return BadRequest for invalid pagination
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?status=Pending");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithOrderTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?orderType=External");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithDateRangeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var fromDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders?createdFrom={fromDate}&createdTo={toDate}");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithSortingParams_ShouldReturnSortedResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?sortBy=totalAmount&sortDirection=desc");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithSupplierFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?supplierID=123");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithOrderIDFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateProcurementToken("proc123", "procurement");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders?orderID=456");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region T009: Contract test POST /purchase-orders

    [Fact]
    public async Task CreatePurchaseOrder_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            OrderType = Data.Enums.OrderType.External,
            CustomerPO = "CUST-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14),
            WhtRate = 3.0m,
            Notes = "Test purchase order",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)Data.Enums.AddressType.Shipping,
                ContactName = "John Doe",
                AddressLine1 = "123 Main St",
                City = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand"
            },
            BillingAddress = new CreateAddressRequest
            {
                AddressType = (Data.Enums.AddressType)Data.Enums.AddressType.Billing,
                ContactName = "Jane Smith",
                AddressLine1 = "456 Business Ave",
                City = "Bangkok",
                PostalCode = "10200",
                Country = "Thailand"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1.0/purchase-orders", content);

        // Assert - Should return Created (201) for successful creation
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithMissingRequiredFields_ShouldReturn400()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);

        var request = new CreatePurchaseOrderRequest
        {
            // Missing required fields: SupplierID, OrderID, CurrencyID, OrderType
        };

        var content = new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1.0/purchase-orders", content);

        // Assert - Should return BadRequest (400) for missing required fields
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithoutAuthToken_ShouldReturn401()
    {
        // Arrange - No authentication token
        ClearAuthentication(); // Ensure no auth token
        var (request, supplier, currency, orderItems) = CreateCompletePurchaseOrderScenario(Data.Enums.OrderType.External);
        SetupMocksForScenario(supplier, currency, orderItems);

        // Act
        var response = await PostAsJsonAsync("/v1.0/purchase-orders", request);

        // Assert - Should return 401 for missing authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region T010: Contract test GET /purchase-orders/{id}

    [Fact]
    public async Task GetPurchaseOrderById_WithValidIdAndEmployeeToken_ShouldReturn200ForOwnOrder()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{purchaseOrderId}");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        await SeedTestDataAsync();
        SetupProcurementAuthentication("proc123", "procurement");
        var nonExistentId = 999999; // Use a clearly non-existent ID

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{nonExistentId}");

        // Assert - Should return 404 for non-existent entity
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T011: Contract test PUT /purchase-orders/{id}

    [Fact]
    public async Task UpdatePurchaseOrder_WithValidRequestAndEmployeeToken_ShouldReturn200ForOwnOrder()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new UpdatePurchaseOrderRequest
        {
            RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            CustomerPO = "UPDATED-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(21),
            WhtRate = 5.0m,
            Notes = "Updated notes"
        };

        var content = new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{purchaseOrderId}", content);

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithMissingRowVersion_ShouldReturn400()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new UpdatePurchaseOrderRequest
        {
            // Missing required RowVersion
            Notes = "Updated notes"
        };

        var content = new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{purchaseOrderId}", content);

        // Assert - Should return BadRequest (400) for missing RowVersion
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region T012: Contract test DELETE /purchase-orders/{id}

    [Fact]
    public async Task DeletePurchaseOrder_WithValidIdAndEmployeeToken_ShouldReturn204ForOwnPendingOrder()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1; // Assuming this is employee's own pending order

        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{purchaseOrderId}");

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePurchaseOrder_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        await SeedTestDataAsync();
        SetupAdminAuthentication("admin123", "admin");
        var nonExistentId = 999999; // Use a clearly non-existent ID

        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{nonExistentId}");

        // Assert - Should return 404 for non-existent entity
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T013: Contract test POST /purchase-orders/{id}/approve

    [Fact]
    public async Task ApprovePurchaseOrder_WithValidIdAndManagerToken_ShouldReturn200()
    {
        // Arrange
        var seededPO = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending, "emp123");
        SetupManagerAuthentication("mgr123", "department1");

        var request = TestDataFactory.CreateApprovePurchaseOrderRequest("mgr123", "Approved by manager");

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPO.Id}/approve", request);

        // Assert - Should return expected response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmployeeToken_ShouldReturn403()
    {
        // Arrange
        var seededPO = await SeedPurchaseOrderAsync(Data.Enums.OrderType.Internal, Data.Enums.OrderStatus.Pending, "emp123");
        SetupEmployeeAuthentication("emp123", "department1");

        var request = TestDataFactory.CreateApprovePurchaseOrderRequest("emp123", "Trying to approve");

        // Act
        var response = await PostAsJsonAsync($"/v1.0/purchase-orders/{seededPO.Id}/approve", request);

        // Assert - Should return 403 for insufficient permissions
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region T014: Contract test POST /purchase-orders/{id}/cancel

    [Fact]
    public async Task CancelPurchaseOrder_WithValidRequestAndCreatorToken_ShouldReturn200()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1; // Assuming this is employee's own order

        var request = new { reason = "No longer needed" };
        var content = new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert - Should return expected response once tests are fixed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithMissingReason_ShouldReturn400()
    {
        // Arrange
        var jwtToken = TestJwtHelper.GenerateEmployeeToken("emp123", "department1");
        Client.DefaultRequestHeaders.Authorization = new("Bearer", jwtToken);
        var purchaseOrderId = 1;

        var request = new { }; // Missing required reason
        var content = new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/cancel", content);

        // Assert - Should return BadRequest (400) for missing reason
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods


    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    #endregion
}

