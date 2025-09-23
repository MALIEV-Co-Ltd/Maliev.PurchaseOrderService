using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration.Contracts;

/// <summary>
/// Contract tests for GET /v1.0/purchase-orders/{id}/addresses endpoint
/// These tests MUST FAIL before implementation - following TDD principles
/// </summary>
public class GetAddressesContractTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl = "/v1.0/purchase-orders";

    public GetAddressesContractTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAddresses_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange & Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    [Fact]
    public async Task GetAddresses_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAddresses_WithValidRequest_ShouldReturn200AndAddressList()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        addresses.Should().BeOfType<List<AddressDto>>();
    }

    [Fact]
    public async Task GetAddresses_WithAddressTypeFilter_ShouldReturn200AndFilteredAddresses()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var purchaseOrderId = 1;
        var addressType = "Shipping";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses/by-type/{addressType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        if (addresses?.Any() == true)
        {
            addresses.Should().OnlyContain(a => a.AddressType.ToString().Equals(addressType, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetAddresses_WithCountryFilter_ShouldReturn200AndFilteredAddresses()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var country = "Thailand";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?country={country}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        if (addresses?.Any() == true)
        {
            addresses.Should().OnlyContain(a => a.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetAddresses_WithCityFilter_ShouldReturn200AndFilteredAddresses()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var city = "Bangkok";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?city={city}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        if (addresses?.Any() == true)
        {
            addresses.Should().OnlyContain(a => a.City.Contains(city, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetAddresses_WithPaginationParameters_ShouldReturn200AndPaginatedResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var page = 1;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?page={page}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<AddressDto>>(responseContent, new JsonSerializerOptions
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
    public async Task GetAddresses_WithInvalidPaginationParameters_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidPage = -1;
        var invalidPageSize = 0;

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?page={invalidPage}&pageSize={invalidPageSize}");

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
    public async Task GetAddresses_WithSortingParameters_ShouldReturn200AndSortedResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var sortBy = "Country";
        var sortOrder = "asc";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?sortBy={sortBy}&sortOrder={sortOrder}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        if (addresses?.Count > 1)
        {
            // Verify sorting is applied (addresses should be in ascending order by country)
            var countries = addresses.Select(a => a.Country).ToList();
            countries.Should().BeInAscendingOrder();
        }
    }

    [Fact]
    public async Task GetAddresses_WithInvalidSortBy_ShouldReturn400()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var invalidSortBy = "InvalidField";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?sortBy={invalidSortBy}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ValidationErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationError.Should().NotBeNull();
        validationError!.Errors.Should().Contain(e => e.Field.Contains("sortBy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAddresses_WithSearchQuery_ShouldReturn200AndFilteredResults()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var searchQuery = "Bangkok";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?search={searchQuery}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        if (addresses?.Any() == true)
        {
            // Verify search is applied - should contain search term in any address field
            if (addresses?.Any() == true)
        {
            // Manual check since LINQ expression trees don't support null propagation
            foreach (var address in addresses)
            {
                var containsSearch = address.ContactName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   address.AddressLine1.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   (address.AddressLine2?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                   address.City.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   (address.StateProvince?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                   address.Country.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                containsSearch.Should().BeTrue($"Address {address.Id} should contain search term '{searchQuery}'");
            }
        }
        }
    }

    [Fact]
    public async Task GetAddresses_WithNoResults_ShouldReturn200AndEmptyArray()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var nonExistentCountry = "NonExistentCountry";

        // Act
        var response = await _client.GetAsync($"{_baseUrl}?country={nonExistentCountry}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        addresses.Should().NotBeNull();
        addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddresses_RoleBasedAccess_EmployeeRole_ShouldReturn200()
    {
        // Arrange
        var employeeToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAddresses_RoleBasedAccess_InvalidRole_ShouldReturn403()
    {
        // Arrange
        var invalidRoleToken = TestJwtHelper.GenerateTestToken("test-user", "InvalidRole");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidRoleToken);

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAddresses_ApiVersioning_ShouldHandleCorrectVersion()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        // This test verifies that the /v1/ path is correctly handled
        response.RequestMessage?.RequestUri?.PathAndQuery.Should().Contain("/v1/");
    }

    [Fact]
    public async Task GetAddresses_WithCacheHeaders_ShouldIncludeCacheControl()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Addresses should be cacheable for performance
        response.Headers.CacheControl?.MaxAge.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetAddresses_ResponseFormat_ShouldIncludeRequiredFields()
    {
        // Arrange
        var validToken = TestJwtHelper.GenerateEmployeeToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var purchaseOrderId = 1;
        var response = await _client.GetAsync($"{_baseUrl}/{purchaseOrderId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var addresses = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (addresses?.Any() == true)
        {
            var firstAddress = addresses.First();
            firstAddress.Id.Should().BeGreaterThan(0);
            firstAddress.ContactName.Should().NotBeNullOrEmpty();
            firstAddress.AddressLine1.Should().NotBeNullOrEmpty();
            firstAddress.City.Should().NotBeNullOrEmpty();
            firstAddress.StateProvince.Should().NotBeNull(); // StateProvince is nullable
            firstAddress.PostalCode.Should().NotBeNullOrEmpty();
            firstAddress.Country.Should().NotBeNullOrEmpty();
            firstAddress.AddressType.Should().BeDefined();
        }
    }

}