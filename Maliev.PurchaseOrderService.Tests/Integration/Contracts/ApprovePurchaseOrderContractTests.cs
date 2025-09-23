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
/// Contract tests for POST /v1.0/purchase-orders/{id}/approve endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class ApprovePurchaseOrderContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public ApprovePurchaseOrderContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;
        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;
        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithValidManagerToken_ShouldReturn200AndApprovedOrder()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var approvedPurchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        approvedPurchaseOrder.Should().NotBeNull();
        approvedPurchaseOrder!.Id.Should().Be(purchaseOrderId);
        approvedPurchaseOrder.Status.Should().Be(Data.Enums.OrderStatus.Approved);
        approvedPurchaseOrder.ApprovedBy.Should().NotBeNullOrEmpty();
        approvedPurchaseOrder.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmployeeRole_ShouldReturn403()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var purchaseOrderId = 1;

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
        errorResponse.Error.Message.Should().Contain("approval permission");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var nonExistentId = 99999;

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{nonExistentId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
        errorResponse.Error.Message.Should().Contain("Purchase order not found");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_AlreadyApprovedOrder_ShouldReturn409()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var alreadyApprovedOrderId = 2; // Assume this order is already approved

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{alreadyApprovedOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Code.Should().Be("ALREADY_APPROVED");
        errorResponse.Error.Message.Should().Contain("already approved");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_CancelledOrder_ShouldReturn409()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var cancelledOrderId = 3; // Assume this order is cancelled

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{cancelledOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Code.Should().Be("INVALID_STATUS_TRANSITION");
        errorResponse.Error.Message.Should().Contain("cannot be approved");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmptyApprovedBy_ShouldReturn400()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var request = CreateValidApprovalRequest();
        request.ApprovedBy = string.Empty; // Empty approver

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Field == "ApprovedBy");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithInvalidContentType_ShouldReturn415()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var xml = "<xml>invalid content type</xml>";
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmptyBody_ShouldReturn400()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithInvalidIdFormat_ShouldReturn400()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var invalidId = "invalid-id";

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{invalidId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithDirectorRole_ShouldApproveHighValueOrders()
    {
        // Arrange
        var directorToken = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directorToken);
        var highValueOrderId = 4; // Assume this is a high-value order requiring director approval

        var request = CreateValidApprovalRequest();
        request.ApprovalLevel = "Director";

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{highValueOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithManagerRoleForHighValueOrder_ShouldReturn403()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var highValueOrderId = 4; // Assume this requires director approval

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{highValueOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Code.Should().Be("INSUFFICIENT_APPROVAL_LEVEL");
        errorResponse.Error.Message.Should().Contain("higher approval level");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_ShouldTriggerPdfGeneration_ForInternalOrders()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var internalOrderId = 5; // Assume this is an internal order

        var request = CreateValidApprovalRequest();
        request.GeneratePdf = true;

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{internalOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // PDF generation should be triggered (to be verified via logs or events)
        response.Headers.Should().ContainKey("X-PDF-Generation-Triggered");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_ShouldSendNotifications_WhenRequested()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var request = CreateValidApprovalRequest();
        request.SendNotifications = true;

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Notifications should be sent (to be verified via logs or events)
        response.Headers.Should().ContainKey("X-Notifications-Sent");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var request = CreateValidApprovalRequest();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1/");
    }

    private ApprovePurchaseOrderRequest CreateValidApprovalRequest()
    {
        return new ApprovePurchaseOrderRequest
        {
            ApprovedBy = "manager@maliev.com",
            Comments = "Approved after review",
            ApprovalLevel = "Manager",
            ApprovedAt = DateTime.UtcNow,
            GeneratePdf = true,
            SendNotifications = true,
            Metadata = new Dictionary<string, string>
            {
                { "approval_source", "web_interface" },
                { "review_notes", "budget_approved" }
            }
        };
    }

}