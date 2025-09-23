using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for POST /v1.0/purchase-orders/{id}/cancel endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class CancelPurchaseOrderContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public CancelPurchaseOrderContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithValidRequest_ShouldReturn200()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var cancelledPurchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        cancelledPurchaseOrder.Should().NotBeNull();
        cancelledPurchaseOrder!.Id.Should().Be(purchaseOrderId);
        cancelledPurchaseOrder.Status.Should().Be(Data.Enums.OrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var nonExistentId = 99999;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{nonExistentId}/cancel", content);

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
    public async Task CancelPurchaseOrder_WithInvalidId_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidId = "invalid";
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{invalidId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithEmptyBody_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithMissingReason_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var invalidRequest = new CancelPurchaseOrderRequest
        {
            // Missing required Reason field
        };
        var json = JsonSerializer.Serialize(invalidRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError.Should().NotBeNull();
        validationError!.Errors.Should().Contain(e => e.Field == "Reason");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithTooLongReason_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var request = new CancelPurchaseOrderRequest
        {
            Reason = new string('A', 1001) // Exceeds 1000 character limit
        };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Field == "Reason");
    }

    [Fact]
    public async Task CancelPurchaseOrder_RoleBasedAccess_ManagerRole_ShouldReturn200()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        var purchaseOrderId = 1;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelPurchaseOrder_RoleBasedAccess_EmployeeRole_ShouldReturn403()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var purchaseOrderId = 1;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelPurchaseOrder_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var request = CreateValidCancelRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1/");
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithInvalidContentType_ShouldReturn415()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var xml = "<xml>invalid content type</xml>";
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/cancel", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    private CancelPurchaseOrderRequest CreateValidCancelRequest()
    {
        return new CancelPurchaseOrderRequest
        {
            Reason = "Order cancelled due to supplier unavailability"
        };
    }

}