using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for PUT /purchaseorders/v1/purchase-orders/{id} endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class UpdatePurchaseOrderContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/purchaseorders/v1/purchase-orders";

    public UpdatePurchaseOrderContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;
        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;
        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithValidRequest_ShouldReturn200AndUpdatedPurchaseOrder()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var updatedPurchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        updatedPurchaseOrder.Should().NotBeNull();
        updatedPurchaseOrder!.Id.Should().Be(purchaseOrderId);
        updatedPurchaseOrder.UpdatedAt.Should().NotBeNull();
        updatedPurchaseOrder.UpdatedBy.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var nonExistentId = 99999;

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{nonExistentId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("Purchase order not found");
        errorResponse.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithMissingRowVersion_ShouldReturn400()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        request.RowVersion = string.Empty; // Missing row version for optimistic concurrency

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Field == "RowVersion");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithStaleRowVersion_ShouldReturn409()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        request.RowVersion = "stale-version"; // Stale version to trigger optimistic concurrency conflict

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Code.Should().Be("CONCURRENCY_CONFLICT");
        errorResponse.Error.Message.Should().Contain("concurrency conflict");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithInvalidWHTRate_ShouldReturn400()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        request.WhtRate = 150.00m; // Invalid WHT rate (> 99.99%)

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

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
    public async Task UpdatePurchaseOrder_WithTooLongCustomerPO_ShouldReturn400()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        request.CustomerPO = new string('A', 51); // Exceeds 50 character limit

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithInvalidContentType_ShouldReturn415()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var xml = "<xml>invalid content type</xml>";
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithEmptyBody_ShouldReturn400()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithInvalidIdFormat_ShouldReturn400()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var invalidId = "invalid-id";

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{invalidId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_RoleBasedAccess_EmployeeRole_ShouldUpdateOwnOrderOnly()
    {
        // Arrange
        var employeeToken = GenerateValidJwtTokenWithRole("Employee"); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var ownOrderId = 1; // Assume this is owned by the employee

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{ownOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_RoleBasedAccess_EmployeeRole_ShouldReturn403ForOthersOrder()
    {
        // Arrange
        var employeeToken = GenerateValidJwtTokenWithRole("Employee"); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var otherUserOrderId = 999; // Assume this belongs to another user

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{otherUserOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_RoleBasedAccess_ManagerRole_ShouldUpdateAnyOrder()
    {
        // Arrange
        var managerToken = GenerateValidJwtTokenWithRole("Manager"); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var anyOrderId = 1;

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{anyOrderId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_ResponseShouldIncludeUpdatedETag()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag?.Tag.Should().NotBeNullOrEmpty();
        response.Headers.ETag?.Tag.Should().NotBe(request.RowVersion);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = GenerateValidJwtToken(); // This will fail - token generation not implemented
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        var request = CreateValidUpdateRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PutAsync($"{_baseUrl}/{purchaseOrderId}", content);

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1/");
    }

    private UpdatePurchaseOrderRequest CreateValidUpdateRequest()
    {
        return new UpdatePurchaseOrderRequest
        {
            RowVersion = "valid-row-version",
            CurrencyID = 2,
            CustomerPO = "UPDATED-CUST-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(45),
            WhtRate = 5.00m,
            Notes = "Updated purchase order notes",
            ShippingAddress = new UpdateAddressRequest
            {
                AddressLine1 = "456 Updated Street",
                City = "Bangkok",
                StateProvince = "Bangkok",
                PostalCode = "10200",
                Country = "Thailand"
            },
            BillingAddress = new UpdateAddressRequest
            {
                AddressLine1 = "789 Updated Billing Street",
                City = "Bangkok",
                StateProvince = "Bangkok",
                PostalCode = "10200",
                Country = "Thailand"
            }
        };
    }

    // This method will intentionally fail - JWT token generation not implemented yet
    private string GenerateValidJwtToken()
    {
        throw new NotImplementedException("JWT token generation not implemented - this test should fail in TDD");
    }

    // This method will intentionally fail - JWT token generation with roles not implemented yet
    private string GenerateValidJwtTokenWithRole(string role)
    {
        throw new NotImplementedException("JWT token generation with roles not implemented - this test should fail in TDD");
    }
}