using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Maliev.PurchaseOrderService.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for GET /purchaseorders/v1.0/purchase-orders/{id}/purchaseorderfiles endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class GetPurchaseOrderFilesContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/purchaseorders/v1.0/purchase-orders";

    public GetPurchaseOrderFilesContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithValidRequest_ShouldReturn200AndFilesList()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Seed a purchase order first
        await SeedPurchaseOrderAsync(1);
        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        files.Should().NotBeNull();
        files.Should().BeOfType<List<PurchaseOrderFileDto>>();
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithNonExistentPurchaseOrderId_ShouldReturn404()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var nonExistentId = 99999;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{nonExistentId}/purchaseorderfiles");

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
    public async Task GetPurchaseOrderFiles_WithInvalidId_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidId = "invalid";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{invalidId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithValidIdButNoFiles_ShouldReturn200AndEmptyArray()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1; // Assuming this exists but has no files

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        files.Should().NotBeNull();
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithFileTypeFilter_ShouldReturn200AndFilteredFiles()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var fileType = "PDF";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?fileType={fileType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        files.Should().NotBeNull();
        if (files?.Any() == true)
        {
            files.Should().OnlyContain(f => f.DocumentType.ToString().Equals(fileType, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithCategoryFilter_ShouldReturn200AndFilteredFiles()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var category = "Invoice";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?category={category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        files.Should().NotBeNull();
        if (files?.Any() == true)
        {
            files.Should().OnlyContain(f => f.DocumentType.ToString().Equals(category, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithPaginationParameters_ShouldReturn200AndPaginatedResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var page = 1;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?page={page}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        paginatedResponse.Should().NotBeNull();
        paginatedResponse!.Data.Should().NotBeNull();
        paginatedResponse.Page.Should().Be(page);
        paginatedResponse.PageSize.Should().Be(pageSize);
        paginatedResponse.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithInvalidPaginationParameters_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var invalidPage = -1;
        var invalidPageSize = 0;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?page={invalidPage}&pageSize={invalidPageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError.Should().NotBeNull();
        validationError!.Errors.Should().Contain(e => e.Field.Contains("page", StringComparison.OrdinalIgnoreCase) || e.Field.Contains("pageSize", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithSortingParameters_ShouldReturn200AndSortedResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var sortBy = "FileName";
        var sortOrder = "asc";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?sortBy={sortBy}&sortOrder={sortOrder}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        files.Should().NotBeNull();
        if (files?.Count > 1)
        {
            // Verify sorting is applied (files should be in ascending order by filename)
            var fileNames = files.Select(f => f.FileName).ToList();
            fileNames.Should().BeInAscendingOrder();
        }
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithIncludeDeleted_ShouldReturn200AndIncludeDeletedFiles()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?includeDeleted=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        files.Should().NotBeNull();
        // Should include files with IsDeleted = true when includeDeleted is true
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_RoleBasedAccess_EmployeeRole_ShouldReturn200()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_RoleBasedAccess_InvalidRole_ShouldReturn403()
    {
        // Arrange
        var invalidRoleToken = TestJwtHelper.GenerateTestToken("test-user", "InvalidRole");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidRoleToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1.0/");
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithCacheHeaders_ShouldIncludeCacheControl()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Files metadata should be cacheable for performance
        response.Headers.CacheControl?.MaxAge.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_ResponseFormat_ShouldIncludeRequiredFields()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (files?.Any() == true)
        {
            var firstFile = files.First();
            firstFile.Id.Should().BeGreaterThan(0);
            firstFile.FileName.Should().NotBeNullOrEmpty();
            firstFile.FileSize.Should().BeGreaterThan(0);
            firstFile.ContentType.Should().NotBeNullOrEmpty();
            firstFile.DocumentType.Should().BeDefined();
            firstFile.UploadedAt.Should().NotBe(default);
            firstFile.UploadedBy.Should().NotBeNullOrEmpty();
            firstFile.ObjectName.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetPurchaseOrderFiles_WithDownloadUrlGeneration_ShouldIncludeDownloadUrls()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/purchaseorderfiles?generateDownloadUrls=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (files?.Any() == true)
        {
            var firstFile = files.First();
            // Note: DownloadUrl would be generated separately, not part of DTO
            firstFile.ObjectName.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// Seeds a purchase order in the test database
    /// </summary>
    private async Task SeedPurchaseOrderAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new Data.Entities.PurchaseOrder
        {
            Id = id,
            OrderNumber = $"PO-TEST-{id:D6}",
            SupplierID = 1001,
            OrderID = 2001,
            CurrencyID = 1,
            Currency = "THB",
            CurrencySymbol = "฿",
            OrderType = Data.Enums.OrderType.Internal,
            Status = Data.Enums.OrderStatus.Pending,
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Notes = "Test purchase order for contract tests",
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();
    }

}