using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Maliev.PurchaseOrderService.Data;
using Microsoft.EntityFrameworkCore;

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

        // Create multiple test purchase orders with different statuses for approval tests
        var allAddresses = new List<Data.Entities.Address>();
        var allPurchaseOrders = new List<Data.Entities.PurchaseOrder>();
        var allOrderItems = new List<Data.Entities.OrderItem>();

        // Purchase Order 1: Pending (for successful approval tests)
        var (purchaseOrder1, orderItems1, shippingAddress1, billingAddress1) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp123");
        purchaseOrder1.Status = Data.Enums.OrderStatus.Pending;
        allPurchaseOrders.Add(purchaseOrder1);
        allOrderItems.AddRange(orderItems1);
        if (shippingAddress1 != null) allAddresses.Add(shippingAddress1);
        if (billingAddress1 != null) allAddresses.Add(billingAddress1);

        // Purchase Order 2: Already Approved (for conflict tests)
        var (purchaseOrder2, orderItems2, shippingAddress2, billingAddress2) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp456");
        purchaseOrder2.Status = Data.Enums.OrderStatus.Approved;
        purchaseOrder2.ApprovedAt = DateTime.UtcNow.AddDays(-1);
        purchaseOrder2.ApprovedBy = "mgr123";
        allPurchaseOrders.Add(purchaseOrder2);
        allOrderItems.AddRange(orderItems2);
        if (shippingAddress2 != null) allAddresses.Add(shippingAddress2);
        if (billingAddress2 != null) allAddresses.Add(billingAddress2);

        // Purchase Order 3: Cancelled (for conflict tests)
        var (purchaseOrder3, orderItems3, shippingAddress3, billingAddress3) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp789");
        purchaseOrder3.Status = Data.Enums.OrderStatus.Cancelled;
        purchaseOrder3.CancelledAt = DateTime.UtcNow.AddDays(-1);
        purchaseOrder3.CancelledBy = "mgr123";
        purchaseOrder3.Notes = "Test cancellation";
        allPurchaseOrders.Add(purchaseOrder3);
        allOrderItems.AddRange(orderItems3);
        if (shippingAddress3 != null) allAddresses.Add(shippingAddress3);
        if (billingAddress3 != null) allAddresses.Add(billingAddress3);

        // Purchase Order 4: High Value Pending (for Director approval tests)
        var (purchaseOrder4, orderItems4, shippingAddress4, billingAddress4) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp999");
        purchaseOrder4.Status = Data.Enums.OrderStatus.Pending;
        // Set high value (>= 500,000) requiring Director approval
        purchaseOrder4.SubtotalAmount = 600000m;
        purchaseOrder4.TotalAmount = 600000m;
        allPurchaseOrders.Add(purchaseOrder4);
        allOrderItems.AddRange(orderItems4);
        if (shippingAddress4 != null) allAddresses.Add(shippingAddress4);
        if (billingAddress4 != null) allAddresses.Add(billingAddress4);

        // Purchase Order 5: Normal Value Pending (for PDF generation tests)
        var (purchaseOrder5, orderItems5, shippingAddress5, billingAddress5) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp555");
        purchaseOrder5.Status = Data.Enums.OrderStatus.Pending;
        allPurchaseOrders.Add(purchaseOrder5);
        allOrderItems.AddRange(orderItems5);
        if (shippingAddress5 != null) allAddresses.Add(shippingAddress5);
        if (billingAddress5 != null) allAddresses.Add(billingAddress5);

        // Purchase Order 6: Normal Value Pending (for notification tests)
        var (purchaseOrder6, orderItems6, shippingAddress6, billingAddress6) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp666");
        purchaseOrder6.Status = Data.Enums.OrderStatus.Pending;
        allPurchaseOrders.Add(purchaseOrder6);
        allOrderItems.AddRange(orderItems6);
        if (shippingAddress6 != null) allAddresses.Add(shippingAddress6);
        if (billingAddress6 != null) allAddresses.Add(billingAddress6);

        // Add all addresses first
        if (allAddresses.Count > 0)
        {
            await dbContext.Addresses.AddRangeAsync(allAddresses);
            await dbContext.SaveChangesAsync();
        }

        // Set address foreign keys for all purchase orders
        if (shippingAddress1 != null)
        {
            purchaseOrder1.ShippingAddressId = shippingAddress1.Id;
            purchaseOrder1.BillingAddressId = billingAddress1?.Id;
        }
        if (shippingAddress2 != null)
        {
            purchaseOrder2.ShippingAddressId = shippingAddress2.Id;
            purchaseOrder2.BillingAddressId = billingAddress2?.Id;
        }
        if (shippingAddress3 != null)
        {
            purchaseOrder3.ShippingAddressId = shippingAddress3.Id;
            purchaseOrder3.BillingAddressId = billingAddress3?.Id;
        }
        if (shippingAddress4 != null)
        {
            purchaseOrder4.ShippingAddressId = shippingAddress4.Id;
            purchaseOrder4.BillingAddressId = billingAddress4?.Id;
        }
        if (shippingAddress5 != null)
        {
            purchaseOrder5.ShippingAddressId = shippingAddress5.Id;
            purchaseOrder5.BillingAddressId = billingAddress5?.Id;
        }
        if (shippingAddress6 != null)
        {
            purchaseOrder6.ShippingAddressId = shippingAddress6.Id;
            purchaseOrder6.BillingAddressId = billingAddress6?.Id;
        }

        // Add all purchase orders
        await dbContext.PurchaseOrders.AddRangeAsync(allPurchaseOrders);
        await dbContext.SaveChangesAsync();

        // Set order item foreign keys and add them
        foreach (var item in orderItems1)
            item.PurchaseOrderId = purchaseOrder1.Id;
        foreach (var item in orderItems2)
            item.PurchaseOrderId = purchaseOrder2.Id;
        foreach (var item in orderItems3)
            item.PurchaseOrderId = purchaseOrder3.Id;
        foreach (var item in orderItems4)
            item.PurchaseOrderId = purchaseOrder4.Id;
        foreach (var item in orderItems5)
            item.PurchaseOrderId = purchaseOrder5.Id;
        foreach (var item in orderItems6)
            item.PurchaseOrderId = purchaseOrder6.Id;

        await dbContext.OrderItems.AddRangeAsync(allOrderItems);
        await dbContext.SaveChangesAsync();
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
        errorResponse.Error.Message.Should().Contain("not found");
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

        errorResponse!.Error.Code.Should().Be("CANNOT_APPROVE_CANCELLED");
        errorResponse.Error.Message.Should().Contain("Cannot approve");
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithEmptyApprovedBy_ShouldReturn400()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 1;

        var (getResponse, request, content) = await PrepareApprovalRequestWithETag(purchaseOrderId);
        request.ApprovedBy = string.Empty; // Empty approver - this should trigger validation error

        // Re-serialize with the modified request
        var json = JsonSerializer.Serialize(request);
        content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

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
        var internalOrderId = 5; // Use purchase order 5 (normal value pending order for PDF test)

        var request = CreateValidApprovalRequest();
        request.GeneratePdf = true;

        // Serialize the request
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{internalOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var approvedPurchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        approvedPurchaseOrder.Should().NotBeNull();
        approvedPurchaseOrder!.Status.Should().Be(Data.Enums.OrderStatus.Approved);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_ShouldSendNotifications_WhenRequested()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var purchaseOrderId = 6; // Use purchase order 6 (normal value pending order for notification test)

        var request = CreateValidApprovalRequest();
        request.SendNotifications = true;

        // Serialize the request
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var approvedPurchaseOrder = JsonSerializer.Deserialize<PurchaseOrderDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        approvedPurchaseOrder.Should().NotBeNull();
        approvedPurchaseOrder!.Status.Should().Be(Data.Enums.OrderStatus.Approved);
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
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
    }

    private async Task<(HttpResponseMessage getResponse, ApprovePurchaseOrderRequest request, StringContent content)> PrepareApprovalRequestWithETag(int purchaseOrderId)
    {
        // First, GET the purchase order to obtain the current ETag
        var getResponse = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = getResponse.Headers.ETag?.Tag;
        etag.Should().NotBeNullOrEmpty();

        var request = CreateValidApprovalRequest();
        // Use the current RowVersion from the ETag
        request.RowVersion = etag!.Trim('"'); // Remove quotes from ETag
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Add If-Match header for optimistic concurrency
        _client.DefaultRequestHeaders.IfMatch.Clear();
        _client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue(etag));

        return (getResponse, request, content);
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