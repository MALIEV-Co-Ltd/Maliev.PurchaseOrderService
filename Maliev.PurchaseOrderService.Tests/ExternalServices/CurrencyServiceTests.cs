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
/// T036: External service mock tests for CurrencyService
/// Tests HTTP client configuration, error handling, circuit breaker patterns,
/// authentication, timeout handling, response parsing and service availability
/// </summary>
public class CurrencyServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<CurrencyServiceClient>> _loggerMock;
    private readonly Mock<IOptions<ExternalServiceOptions>> _optionsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ExternalServiceOptions _serviceOptions;

    public CurrencyServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<CurrencyServiceClient>>();
        _optionsMock = new Mock<IOptions<ExternalServiceOptions>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _serviceOptions = new ExternalServiceOptions
        {
            CurrencyService = new ServiceEndpoint
            {
                BaseUrl = "https://test.api.maliev.com/currency",
                TimeoutInSeconds = 15
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_serviceOptions);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_serviceOptions.CurrencyService.BaseUrl)
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(_httpClient);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ValidCurrencies_ReturnsExchangeRate()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "THB";
        var expectedRate = new ExchangeRateDto
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = 35.25m,
            RateDate = DateTime.UtcNow,
            Source = "Bank of Thailand"
        };

        var responseContent = JsonSerializer.Serialize(expectedRate);
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
                    req.RequestUri!.ToString().Contains($"/exchange-rate/{fromCurrency}/{toCurrency}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetExchangeRateAsync(fromCurrency, toCurrency);

        // Assert
        result.Should().NotBeNull();
        result.FromCurrency.Should().Be(fromCurrency);
        result.ToCurrency.Should().Be(toCurrency);
        result.Rate.Should().Be(35.25m);
        result.Source.Should().Be("Bank of Thailand");
    }

    [Fact]
    public async Task ConvertCurrencyAsync_ValidInput_ReturnsConvertedAmount()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "EUR";
        var amount = 1000m;
        var expectedConversion = new CurrencyConversionDto
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            OriginalAmount = amount,
            ConvertedAmount = 850.50m,
            ExchangeRate = 0.8505m,
            ConversionDate = DateTime.UtcNow
        };

        var responseContent = JsonSerializer.Serialize(expectedConversion);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);

        // Assert
        result.Should().NotBeNull();
        result.OriginalAmount.Should().Be(amount);
        result.ConvertedAmount.Should().Be(850.50m);
        result.ExchangeRate.Should().Be(0.8505m);
    }

    [Fact]
    public async Task GetSupportedCurrenciesAsync_ValidRequest_ReturnsCurrencyList()
    {
        // Arrange
        var expectedCurrencies = new
        {
            Currencies = new[]
            {
                new { Code = "USD", Name = "US Dollar", Symbol = "$", IsActive = true },
                new { Code = "EUR", Name = "Euro", Symbol = "€", IsActive = true },
                new { Code = "THB", Name = "Thai Baht", Symbol = "฿", IsActive = true },
                new { Code = "GBP", Name = "British Pound", Symbol = "£", IsActive = true }
            },
            LastUpdated = DateTime.UtcNow
        };

        var responseContent = JsonSerializer.Serialize(expectedCurrencies);
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
                    req.RequestUri!.ToString().Contains("/currencies")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetSupportedCurrenciesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain(c => c.Code == "USD");
        result.Should().Contain(c => c.Code == "EUR");
        result.Should().AllSatisfy(c => c.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ValidPeriod_ReturnsHistoricalData()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "JPY";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var expectedHistorical = new
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            StartDate = startDate,
            EndDate = endDate,
            Rates = new[]
            {
                new { Date = DateTime.UtcNow.AddDays(-1), Rate = 150.25m },
                new { Date = DateTime.UtcNow.AddDays(-2), Rate = 149.80m },
                new { Date = DateTime.UtcNow.AddDays(-3), Rate = 151.15m }
            }
        };

        var responseContent = JsonSerializer.Serialize(expectedHistorical);
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
                    req.RequestUri!.ToString().Contains("/historical") &&
                    req.RequestUri.ToString().Contains(fromCurrency) &&
                    req.RequestUri.ToString().Contains(toCurrency)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetHistoricalExchangeRatesAsync(fromCurrency, toCurrency, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.First().FromCurrency.Should().Be(fromCurrency);
        result.First().ToCurrency.Should().Be(toCurrency);
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ValidateCurrencyCodeAsync_ValidCode_ReturnsTrue()
    {
        // Arrange
        var currencyCode = "USD";
        var validationResponse = new CurrencyValidationDto
        {
            CurrencyCode = currencyCode,
            IsValid = true,
            IsSupported = true,
            Message = "Valid currency",
            CurrencyInfo = new CurrencyDto { Code = currencyCode, Name = "US Dollar" }
        };

        var responseContent = JsonSerializer.Serialize(validationResponse);
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
                    req.RequestUri!.ToString().Contains($"/validate/{currencyCode}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.ValidateCurrencyAsync(currencyCode);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("USD");
    }

    [Fact]
    public async Task ValidateCurrencyCodeAsync_InvalidCode_ReturnsFalse()
    {
        // Arrange
        var currencyCode = "XYZ";
        var validationResponse = new CurrencyValidationDto
        {
            CurrencyCode = currencyCode,
            IsValid = false,
            IsSupported = false,
            Message = "Invalid currency code",
            CurrencyInfo = null
        };

        var responseContent = JsonSerializer.Serialize(validationResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.ValidateCurrencyAsync(currencyCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExchangeRateAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "EUR";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetExchangeRateAsync(fromCurrency, toCurrency));

        exception.Message.Should().Contain("CurrencyService");
    }

    [Fact]
    public async Task ConvertCurrencyAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "EUR";
        var amount = 100m;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.ConvertCurrencyAsync(amount, fromCurrency, toCurrency));

        exception.Message.Should().Contain("Timeout while converting currency");
    }

    [Fact]
    public async Task GetExchangeRateAsync_InvalidCurrencyPair_ThrowsBadRequestException()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "INVALID";
        var errorResponse = new { message = "Invalid currency pair", code = "INVALID_CURRENCY" };
        var responseContent = JsonSerializer.Serialize(errorResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetExchangeRateAsync(fromCurrency, toCurrency));

        exception.Message.Should().Contain("CurrencyService error");
    }

    [Fact]
    public async Task GetExchangeRateAsync_CircuitBreakerOpen_ThrowsCircuitBreakerException()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "EUR";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Circuit breaker is open for CurrencyService"));

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetExchangeRateAsync(fromCurrency, toCurrency));

        exception.Message.Should().Contain("CurrencyService");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetExchangeRateAsync_AuthenticationFailure_ThrowsAuthenticationException(HttpStatusCode statusCode)
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "EUR";
        var httpResponse = new HttpResponseMessage(statusCode);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetExchangeRateAsync(fromCurrency, toCurrency));

        exception.Message.Should().Contain("authentication");
    }

    [Fact]
    public async Task GetExchangeRateAsync_RateNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var fromCurrency = "USD";
        var toCurrency = "RARE";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetExchangeRateAsync(fromCurrency, toCurrency));

        exception.Message.Should().Contain("Exchange rate not found");
    }

    [Fact]
    public async Task GetSupportedCurrenciesAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("malformed json response", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new CurrencyServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.GetSupportedCurrenciesAsync());

        exception.Message.Should().Contain("parsing");
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrencyServiceClient(null!, _loggerMock.Object, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrencyServiceClient(_httpClient, null!, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrencyServiceClient(_httpClient, _loggerMock.Object, null!));
    }
}