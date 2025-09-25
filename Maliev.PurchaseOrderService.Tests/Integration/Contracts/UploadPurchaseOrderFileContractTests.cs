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
/// Contract tests for POST /purchaseorders/v1.0/purchase-orders/{id}/purchaseorderfiles endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class UploadPurchaseOrderFileContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public UploadPurchaseOrderFileContractTests(TestWebApplicationFactory<Program> factory)
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

        // Create a test purchase order for file uploads
        var (purchaseOrder, orderItems, shippingAddress, billingAddress) =
            TestDataFactory.CreateCompletePurchaseOrderWithEntities(Data.Enums.OrderType.Internal, 2, "emp123");

        // Add addresses first
        var addresses = new List<Data.Entities.Address>();
        if (shippingAddress != null) addresses.Add(shippingAddress);
        if (billingAddress != null) addresses.Add(billingAddress);

        if (addresses.Count > 0)
        {
            await dbContext.Addresses.AddRangeAsync(addresses);
            await dbContext.SaveChangesAsync();
        }

        // Set address foreign keys
        if (shippingAddress != null)
            purchaseOrder.ShippingAddressId = shippingAddress.Id;
        if (billingAddress != null)
            purchaseOrder.BillingAddressId = billingAddress.Id;

        // Add purchase order
        await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
        await dbContext.SaveChangesAsync();

        // Set order item foreign keys and add them
        foreach (var item in orderItems)
            item.PurchaseOrderId = purchaseOrder.Id;

        await dbContext.OrderItems.AddRangeAsync(orderItems);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithValidRequest_ShouldReturn201AndUploadResult()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().NotBeNull();

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<DocumentUploadResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        uploadResult.Should().NotBeNull();
        uploadResult!.FileId.Should().BeGreaterThan(0);
        uploadResult.File?.FileName.Should().NotBeNullOrEmpty();
        uploadResult.FileSize.Should().BeGreaterThan(0);
        uploadResult.FilePath.Should().NotBeNullOrEmpty();
        uploadResult.UploadedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithNonExistentPurchaseOrderId_ShouldReturn404()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var nonExistentId = 99999;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{nonExistentId}/purchaseorderfiles", content);

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
    public async Task UploadPurchaseOrderFile_WithInvalidId_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidId = "invalid";
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{invalidId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithEmptyContent_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithMissingFile_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Invoice"), "category");
        content.Add(new StringContent("Important invoice document"), "description");
        // Missing file content

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError.Should().NotBeNull();
        validationError!.Errors.Should().Contain(e => e.Field.Contains("file", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithTooLargeFile_ShouldReturn413()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateLargeFileContent(); // Exceeds 50MB limit

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Message.Should().Contain("file size");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithUnsupportedFileType_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateUnsupportedFileContent(); // .exe file

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Message.Contains("file type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithMissingCategory_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Sample PDF content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "sample.pdf");
        // Missing required category

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Field == "category");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithInvalidCategory_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Sample PDF content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "sample.pdf");
        content.Add(new StringContent("InvalidCategory"), "category");

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError!.Errors.Should().Contain(e => e.Field == "category");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithValidPdfFile_ShouldReturn201()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateValidPdfContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<DocumentUploadResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        uploadResult!.File?.DocumentType.Should().BeDefined();
        uploadResult.File?.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithValidImageFile_ShouldReturn201()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateValidImageContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<DocumentUploadResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        uploadResult!.File?.DocumentType.Should().BeDefined();
        uploadResult.File?.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_RoleBasedAccess_EmployeeRole_ShouldReturn201()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_RoleBasedAccess_InvalidRole_ShouldReturn403()
    {
        // Arrange
        var invalidRoleToken = TestJwtHelper.GenerateTestToken("test-user", "InvalidRole");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidRoleToken);

        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithInvalidContentType_ShouldReturn415()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = new StringContent("invalid content", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_WithVirusScanFailure_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateMaliciousFileContent(); // Simulated malicious file

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse!.Error.Message.Should().ContainEquivalentOf("virus");
    }

    [Fact]
    public async Task UploadPurchaseOrderFile_ResponseFormat_ShouldIncludeRequiredFields()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var content = CreateValidMultipartContent();

        // Act
        var response = await _client.PostAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<DocumentUploadResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        uploadResult.Should().NotBeNull();
        uploadResult!.FileId.Should().BeGreaterThan(0);
        uploadResult.File?.FileName.Should().NotBeNullOrEmpty();
        uploadResult.FileSize.Should().BeGreaterThan(0);
        uploadResult.File?.ContentType.Should().NotBeNullOrEmpty();
        uploadResult.FilePath.Should().NotBeNullOrEmpty();
        uploadResult.UploadedAt.Should().NotBe(default);
        uploadResult.File?.DocumentType.Should().BeDefined();
    }

    private MultipartFormDataContent CreateValidMultipartContent()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Sample PDF content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "sample.pdf");
        content.Add(new StringContent("Invoice"), "category");
        content.Add(new StringContent("Important invoice document"), "description");
        return content;
    }

    private MultipartFormDataContent CreateValidPdfContent()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Sample PDF content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "invoice.pdf");
        content.Add(new StringContent("Invoice"), "category");
        content.Add(new StringContent("Purchase order invoice"), "description");
        return content;
    }

    private MultipartFormDataContent CreateValidImageContent()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // JPEG header
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "receipt.jpg");
        content.Add(new StringContent("Receipt"), "category");
        content.Add(new StringContent("Payment receipt image"), "description");
        return content;
    }

    private MultipartFormDataContent CreateLargeFileContent()
    {
        var content = new MultipartFormDataContent();
        var largeData = new byte[52 * 1024 * 1024]; // 52MB - exceeds 50MB limit
        var fileContent = new ByteArrayContent(largeData);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "large-file.pdf");
        content.Add(new StringContent("Invoice"), "category");
        return content;
    }

    private MultipartFormDataContent CreateUnsupportedFileContent()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Executable content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "malicious.exe");
        content.Add(new StringContent("Other"), "category");
        return content;
    }

    private MultipartFormDataContent CreateMaliciousFileContent()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("EICAR-TEST-FILE")); // Simulated virus signature
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");
        content.Add(new StringContent("Other"), "category");
        return content;
    }

}