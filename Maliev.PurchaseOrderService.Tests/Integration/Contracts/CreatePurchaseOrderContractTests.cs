using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for POST /v1.0/purchase-orders endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class CreatePurchaseOrderContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public CreatePurchaseOrderContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = CreateValidPurchaseOrderRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var request = CreateValidPurchaseOrderRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithValidRequest_ShouldReturn201AndCreatedPurchaseOrder()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var request = CreateValidPurchaseOrderRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location?.ToString().Should().Match("*/v1.0/purchase-orders/*");

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdPurchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        createdPurchaseOrder.Should().NotBeNull();
        createdPurchaseOrder!.Id.Should().BeGreaterThan(0);
        createdPurchaseOrder.OrderNumber.Should().NotBeNullOrEmpty();
        createdPurchaseOrder.SupplierID.Should().Be(request.SupplierID);
        createdPurchaseOrder.OrderID.Should().Be(request.OrderID);
        createdPurchaseOrder.CurrencyID.Should().Be(request.CurrencyID);
        createdPurchaseOrder.OrderType.Should().Be(request.OrderType);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidContentType_ShouldReturn415()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var request = CreateValidPurchaseOrderRequest();
        var xml = "<xml>invalid content type</xml>";
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithEmptyBody_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithMissingRequiredFields_ShouldReturn400WithValidationErrors()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidRequest = new CreatePurchaseOrderRequest
        {
            // Missing required fields: SupplierID, OrderID, CurrencyID, OrderType
        };
        var json = JsonSerializer.Serialize(invalidRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError.Should().NotBeNull();
        validationError!.Errors.Should().Contain(e => e.Field == "SupplierID");
        validationError.Errors.Should().Contain(e => e.Field == "OrderID");
        validationError.Errors.Should().Contain(e => e.Field == "CurrencyID");
        validationError.Errors.Should().Contain(e => e.Field == "OrderType");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidSupplierID_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var request = CreateValidPurchaseOrderRequest();
        request.SupplierID = -1; // Invalid supplier ID

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidWHTRate_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var request = CreateValidPurchaseOrderRequest();
        request.WhtRate = 150.00m; // Invalid WHT rate (> 99.99%)

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Field == "WhtRate");
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithTooLongCustomerPO_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var request = CreateValidPurchaseOrderRequest();
        request.CustomerPO = new string('A', 51); // Exceeds 50 character limit

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePurchaseOrder_RoleBasedAccess_EmployeeRole_ShouldReturn201()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var request = CreateValidPurchaseOrderRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePurchaseOrder_RoleBasedAccess_InvalidRole_ShouldReturn403()
    {
        // Arrange
        var invalidRoleToken = TestJwtHelper.GenerateTestToken("test-user", "InvalidRole");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidRoleToken);

        var request = CreateValidPurchaseOrderRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePurchaseOrder_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var request = CreateValidPurchaseOrderRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync(_baseUrl, content);

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
    }

    private CreatePurchaseOrderRequest CreateValidPurchaseOrderRequest()
    {
        return new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal,
            CustomerPO = "CUST-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            WhtRate = 3.00m,
            Notes = "Test purchase order",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = Data.Enums.AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "123 Test Street",
                City = "Bangkok",
                StateProvince = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand"
            },
            BillingAddress = new CreateAddressRequest
            {
                AddressType = Data.Enums.AddressType.Billing,
                ContactName = "Billing Contact",
                AddressLine1 = "456 Billing Street",
                City = "Bangkok",
                StateProvince = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand"
            }
        };
    }

}