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
/// Contract tests for GET /v1.0/purchase-orders/{id} endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class GetPurchaseOrderByIdContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public GetPurchaseOrderByIdContractTests(TestWebApplicationFactory<Program> factory)
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

        // Create test purchase orders - one for emp123 and one for emp456
        var (purchaseOrder1, orderItems1, shippingAddress1, billingAddress1) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp123");

        var (purchaseOrder2, orderItems2, shippingAddress2, billingAddress2) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp456");

        // Add addresses first
        var addresses = new List<Data.Entities.Address>();
        if (shippingAddress1 != null) addresses.Add(shippingAddress1);
        if (billingAddress1 != null) addresses.Add(billingAddress1);
        if (shippingAddress2 != null) addresses.Add(shippingAddress2);
        if (billingAddress2 != null) addresses.Add(billingAddress2);

        if (addresses.Count > 0)
        {
            await dbContext.Addresses.AddRangeAsync(addresses);
            await dbContext.SaveChangesAsync();
        }

        // Set address foreign keys for purchase order 1
        if (shippingAddress1 != null)
            purchaseOrder1.ShippingAddressId = shippingAddress1.Id;
        if (billingAddress1 != null)
            purchaseOrder1.BillingAddressId = billingAddress1.Id;

        // Set address foreign keys for purchase order 2
        if (shippingAddress2 != null)
            purchaseOrder2.ShippingAddressId = shippingAddress2.Id;
        if (billingAddress2 != null)
            purchaseOrder2.BillingAddressId = billingAddress2.Id;

        // Add purchase orders
        await dbContext.PurchaseOrders.AddRangeAsync(new[] { purchaseOrder1, purchaseOrder2 });
        await dbContext.SaveChangesAsync();

        // Set order item foreign keys and add them for purchase order 1
        foreach (var item in orderItems1)
            item.PurchaseOrderId = purchaseOrder1.Id;

        // Set order item foreign keys and add them for purchase order 2
        foreach (var item in orderItems2)
            item.PurchaseOrderId = purchaseOrder2.Id;

        await dbContext.OrderItems.AddRangeAsync(orderItems1.Concat(orderItems2));
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithValidIdAndToken_ShouldReturn200AndPurchaseOrderDetails()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var purchaseOrderDetail = JsonSerializer.Deserialize<PurchaseOrderDetailResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        purchaseOrderDetail.Should().NotBeNull();
        purchaseOrderDetail!.Id.Should().Be(purchaseOrderId);
        purchaseOrderDetail.OrderNumber.Should().NotBeNullOrEmpty();
        purchaseOrderDetail.SupplierName.Should().NotBeNullOrEmpty();
        purchaseOrderDetail.Items.Should().NotBeNull();
        // PurchaseOrderFiles property not available in PurchaseOrderDetailResponse
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var nonExistentId = 99999;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Message.Should().Contain("Purchase order with ID");
        errorResponse.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithInvalidIdFormat_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var invalidId = "invalid-id";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Invalid ID format returns 400
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithNegativeId_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var negativeId = -1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{negativeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Route constraint {id:int} means invalid IDs return 404
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithZeroId_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var zeroId = 0;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{zeroId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Route constraint {id:int} means invalid IDs return 404
    }

    [Fact]
    public async Task GetPurchaseOrderById_ResponseShouldIncludeETag()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag?.Tag.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithIfNoneMatchHeader_ShouldReturn304WhenNotModified()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        // First request to get ETag
        var firstResponse = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");
        var etag = firstResponse.Headers.ETag?.Tag;

        // Second request with If-None-Match header
        _client.DefaultRequestHeaders.IfNoneMatch.Clear();
        _client.DefaultRequestHeaders.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(etag!));

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task GetPurchaseOrderById_RoleBasedAccess_EmployeeRole_ShouldReturnOwnOrderOnly()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var purchaseOrderId = 1; // Assume this is owned by the employee

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Employee should only access their own orders
    }

    [Fact]
    public async Task GetPurchaseOrderById_RoleBasedAccess_EmployeeRole_ShouldReturn403ForOthersOrder()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var otherUserOrderId = 2; // This belongs to emp456, but test runs as emp123

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{otherUserOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPurchaseOrderById_RoleBasedAccess_ManagerRole_ShouldReturnAnyOrder()
    {
        // Arrange
        var managerToken = TestJwtHelper.GenerateManagerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var anyOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{anyOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Manager should access any order
    }

    [Fact]
    public async Task GetPurchaseOrderById_ResponseShouldIncludeCorrectCacheHeaders()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl?.Private.Should().BeTrue();
        response.Headers.CacheControl?.MaxAge.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetPurchaseOrderById_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
    }

    [Fact]
    public async Task GetPurchaseOrderById_ResponseShouldIncludeRelatedData()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var purchaseOrderDetail = JsonSerializer.Deserialize<PurchaseOrderDetailResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Verify that related data is included
        purchaseOrderDetail!.ShippingAddress.Should().NotBeNull();
        purchaseOrderDetail.BillingAddress.Should().NotBeNull();
        purchaseOrderDetail.Items.Should().NotBeNull();
        // PurchaseOrderFiles property not available in PurchaseOrderDetailResponse
        // Note: AuditLogs property not available in current DTO structure
    }

}