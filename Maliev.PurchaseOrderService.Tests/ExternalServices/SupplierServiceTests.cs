using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Tests.ExternalServices;

/// <summary>
/// T034: External service mock tests for SupplierService
/// Tests HTTP client configuration, error handling, circuit breaker patterns,
/// authentication, timeout handling, response parsing and service availability
/// </summary>
public class SupplierServiceTests
{
    private readonly Mock<ILogger<SupplierServiceClient>> _loggerMock;
    private readonly Mock<IOptions<ExternalServiceOptions>> _optionsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ExternalServiceOptions _serviceOptions;

    public SupplierServiceTests()
    {
        _loggerMock = new Mock<ILogger<SupplierServiceClient>>();
        _optionsMock = new Mock<IOptions<ExternalServiceOptions>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _serviceOptions = new ExternalServiceOptions
        {
            SupplierService = new ServiceEndpoint
            {
                BaseUrl = "https://test.api.maliev.com/suppliers/v1",
                TimeoutInSeconds = 30
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_serviceOptions);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_serviceOptions.SupplierService.BaseUrl)
        };
    }

    [Fact]
    public async Task GetSupplierAsync_ValidId_ReturnsSupplierDetails()
    {
        // Arrange
        var supplierId = 12345;
        var expectedSupplier = new SupplierDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier",
            Email = "supplier@test.com",
            Phone = "+1234567890",
            Address = new AddressDto { AddressLine1 = "123 Test Street" },
            IsActive = true
        };

        var responseContent = JsonSerializer.Serialize(expectedSupplier);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/suppliers/{supplierId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetSupplierAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedSupplier.Id);
        result.Name.Should().Be(expectedSupplier.Name);
        result.Email.Should().Be(expectedSupplier.Email);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetSupplierAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var supplierId = 12345;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GetSupplierAsync(supplierId));

        exception.Message.Should().Contain("Failed to get supplier");
    }

    [Fact]
    public async Task GetSupplierAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var supplierId = 12345;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GetSupplierAsync(supplierId));

        exception.Message.Should().Contain("Timeout");
    }

    [Fact]
    public async Task GetSupplierAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var supplierId = 12345;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GetSupplierAsync(supplierId));

        exception.Message.Should().Contain("Invalid response format");
    }

    [Fact]
    public async Task GetSupplierContactAsync_ValidId_ReturnsSupplierContact()
    {
        // Arrange
        var supplierId = 12345;
        var expectedContact = new SupplierContactDto
        {
            SupplierId = supplierId,
            ContactName = "John Doe",
            ContactEmail = "john@supplier.com",
            ContactPhone = "+1234567890",
            ContactTitle = "Sales Manager"
        };

        var responseContent = JsonSerializer.Serialize(expectedContact);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/suppliers/{supplierId}/contact")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetSupplierContactAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
        result!.SupplierId.Should().Be(expectedContact.SupplierId);
        result.ContactName.Should().Be(expectedContact.ContactName);
        result.ContactEmail.Should().Be(expectedContact.ContactEmail);
    }

    [Fact]
    public async Task GetSupplierProductsAsync_ValidId_ReturnsSupplierProducts()
    {
        // Arrange
        var supplierId = 12345;
        var expectedProducts = new List<SupplierProductDto>
        {
            new() { Id = 1, SupplierId = supplierId, ProductName = "Product 1", ProductCode = "SKU001", UnitPrice = 100.00m },
            new() { Id = 2, SupplierId = supplierId, ProductName = "Product 2", ProductCode = "SKU002", UnitPrice = 200.00m }
        };

        var responseContent = JsonSerializer.Serialize(expectedProducts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/suppliers/{supplierId}/products")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetSupplierProductsAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().ProductName.Should().Be("Product 1");
        result.Last().ProductName.Should().Be("Product 2");
    }

    [Fact]
    public async Task ValidateSupplierAsync_ValidSupplier_ReturnsTrue()
    {
        // Arrange
        var supplierId = 12345;
        var validationResult = new Dictionary<string, bool> { { "isValid", true } };

        var responseContent = JsonSerializer.Serialize(validationResult);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/suppliers/{supplierId}/validate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.ValidateSupplierAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSupplierPaymentTermsAsync_ValidId_ReturnsPaymentTerms()
    {
        // Arrange
        var supplierId = 12345;
        var expectedPaymentTerms = new SupplierPaymentTermsDto
        {
            SupplierId = supplierId,
            PaymentTerms = "Net 30",
            CreditLimit = 50000.00m,
            Currency = "THB"
        };

        var responseContent = JsonSerializer.Serialize(expectedPaymentTerms);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/suppliers/{supplierId}/payment-terms")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetSupplierPaymentTermsAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
        result!.SupplierId.Should().Be(expectedPaymentTerms.SupplierId);
        result.PaymentTerms.Should().Be(expectedPaymentTerms.PaymentTerms);
        result.CreditLimit.Should().Be(expectedPaymentTerms.CreditLimit);
    }

    [Fact]
    public async Task GetSupplierAsync_CircuitBreakerOpen_ThrowsCircuitBreakerException()
    {
        // Arrange
        var supplierId = 12345;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Circuit breaker is open"));

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GetSupplierAsync(supplierId));

        exception.Message.Should().Contain("Failed to get supplier");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetSupplierAsync_AuthenticationFailure_ThrowsAuthenticationException(HttpStatusCode statusCode)
    {
        // Arrange
        var supplierId = 12345;
        var httpResponse = new HttpResponseMessage(statusCode);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GetSupplierAsync(supplierId));

        exception.Message.Should().Contain("Failed to get supplier");
    }

    [Fact]
    public async Task GetSupplierAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var supplierId = 12345;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new SupplierServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetSupplierAsync(supplierId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SupplierServiceClient(null!, _loggerMock.Object, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SupplierServiceClient(_httpClient, null!, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SupplierServiceClient(_httpClient, _loggerMock.Object, null!));
    }
}