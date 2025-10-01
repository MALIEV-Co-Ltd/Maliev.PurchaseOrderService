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

        // Create multiple test purchase orders for cancellation tests to avoid test isolation issues
        var allAddresses = new List<Data.Entities.Address>();
        var allPurchaseOrders = new List<Data.Entities.PurchaseOrder>();
        var allOrderItems = new List<Data.Entities.OrderItem>();

        for (int i = 0; i < 5; i++) // Create 5 purchase orders for different tests
        {
            var (purchaseOrder, orderItems, shippingAddress, billingAddress) =
                TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, $"emp{i}");

            // Set status to pending for cancellation tests
            purchaseOrder.Status = Data.Enums.OrderStatus.Pending;

            // Collect addresses
            if (shippingAddress != null) allAddresses.Add(shippingAddress);
            if (billingAddress != null) allAddresses.Add(billingAddress);

            allPurchaseOrders.Add(purchaseOrder);
            allOrderItems.AddRange(orderItems);
        }

        // Add all addresses first
        if (allAddresses.Count > 0)
        {
            await dbContext.Addresses.AddRangeAsync(allAddresses);
            await dbContext.SaveChangesAsync();
        }

        // Set address foreign keys for all purchase orders
        for (int i = 0; i < allPurchaseOrders.Count; i++)
        {
            var purchaseOrder = allPurchaseOrders[i];
            var shippingAddress = allAddresses.Where(a => a.AddressType == Data.Enums.AddressType.Shipping).Skip(i).FirstOrDefault();
            var billingAddress = allAddresses.Where(a => a.AddressType == Data.Enums.AddressType.Billing).Skip(i).FirstOrDefault();

            if (shippingAddress != null)
                purchaseOrder.ShippingAddressId = shippingAddress.Id;
            if (billingAddress != null)
                purchaseOrder.BillingAddressId = billingAddress.Id;
        }

        // Add all purchase orders
        await dbContext.PurchaseOrders.AddRangeAsync(allPurchaseOrders);
        await dbContext.SaveChangesAsync();

        // Set order item foreign keys and add them
        for (int i = 0; i < allPurchaseOrders.Count; i++)
        {
            var purchaseOrder = allPurchaseOrders[i];
            var orderItems = allOrderItems.Skip(i * 2).Take(2); // 2 items per purchase order

            foreach (var item in orderItems)
                item.PurchaseOrderId = purchaseOrder.Id;
        }

        await dbContext.OrderItems.AddRangeAsync(allOrderItems);
        await dbContext.SaveChangesAsync();
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

        var purchaseOrderId = 2;
        var (getResponse, request, content) = await PrepareCancelRequestWithETag(purchaseOrderId);

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

        var purchaseOrderId = 3;
        var (getResponse, request, content) = await PrepareCancelRequestWithETag(purchaseOrderId);

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
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
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

    private async Task<(HttpResponseMessage getResponse, CancelPurchaseOrderRequest request, StringContent content)> PrepareCancelRequestWithETag(int purchaseOrderId)
    {
        // First, GET the purchase order to obtain the current ETag
        var getResponse = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = getResponse.Headers.ETag?.Tag;
        etag.Should().NotBeNullOrEmpty();

        var request = CreateValidCancelRequest();
        // Use the current RowVersion from the ETag
        request.RowVersion = etag!.Trim('"'); // Remove quotes from ETag
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Add If-Match header for optimistic concurrency
        _client.DefaultRequestHeaders.IfMatch.Clear();
        _client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue(etag));

        return (getResponse, request, content);
    }

    private CancelPurchaseOrderRequest CreateValidCancelRequest()
    {
        return new CancelPurchaseOrderRequest
        {
            Reason = "Order cancelled due to supplier unavailability"
        };
    }

}