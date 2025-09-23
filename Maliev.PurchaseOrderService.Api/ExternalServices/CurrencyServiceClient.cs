using System.IO;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// HTTP client implementation for Currency Service integration
/// Handles currency validation and exchange rate caching
/// </summary>
public class CurrencyServiceClient : ICurrencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyServiceClient> _logger;
    private readonly ExternalServiceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public CurrencyServiceClient(
        HttpClient httpClient,
        ILogger<CurrencyServiceClient> logger,
        IOptions<ExternalServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<ExchangeRateDto?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                throw new ArgumentException("Currency codes cannot be null or empty");
            }

            _logger.LogInformation("Getting exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);

            var response = await _httpClient.GetAsync($"/exchange-rate/{fromCurrency}/{toCurrency}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Exchange rate not found for {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
                throw new InvalidOperationException($"Exchange rate not found for {fromCurrency} to {toCurrency}");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError("Authentication failure while getting exchange rate {FromCurrency} to {ToCurrency}",
                    fromCurrency, toCurrency);
                throw new UnauthorizedAccessException($"Currency service authentication failed while getting exchange rate {fromCurrency} to {toCurrency}");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogError("Invalid request for exchange rate {FromCurrency} to {ToCurrency}",
                    fromCurrency, toCurrency);
                throw new HttpRequestException($"CurrencyService error while getting exchange rate {fromCurrency} to {toCurrency}: Bad Request");
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "CurrencyService HTTP error occurred while getting exchange rate {FromCurrency} to {ToCurrency}",
                    fromCurrency, toCurrency);
                throw new HttpRequestException($"CurrencyService error while getting exchange rate {fromCurrency} to {toCurrency}: {ex.Message}", ex);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var exchangeRate = JsonSerializer.Deserialize<ExchangeRateDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved exchange rate from {FromCurrency} to {ToCurrency}: {Rate}",
                fromCurrency, toCurrency, exchangeRate?.Rate);
            return exchangeRate;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting exchange rate {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new TimeoutException($"Timeout while getting exchange rate {fromCurrency} to {toCurrency}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting exchange rate {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new InvalidDataException($"Invalid response format while getting exchange rate {fromCurrency} to {toCurrency}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CurrencyConversionDto?> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        try
        {
            if (amount < 0)
            {
                throw new ArgumentException("Amount cannot be negative", nameof(amount));
            }

            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                throw new ArgumentException("Currency codes cannot be null or empty");
            }

            _logger.LogInformation("Converting {Amount} from {FromCurrency} to {ToCurrency}",
                amount, fromCurrency, toCurrency);

            var requestData = new
            {
                Amount = amount,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/convert", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Currency conversion not available for {FromCurrency} to {ToCurrency}",
                    fromCurrency, toCurrency);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var conversion = JsonSerializer.Deserialize<CurrencyConversionDto>(responseContent, _jsonOptions);

            _logger.LogInformation("Successfully converted {Amount} {FromCurrency} to {ConvertedAmount} {ToCurrency}",
                amount, fromCurrency, conversion?.ConvertedAmount, toCurrency);
            return conversion;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while converting currency {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Failed to convert currency {fromCurrency} to {toCurrency}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while converting currency {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Timeout while converting currency {fromCurrency} to {toCurrency}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while converting currency {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Invalid response format while converting currency {fromCurrency} to {toCurrency}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CurrencyDto>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting supported currencies");

            var response = await _httpClient.GetAsync("/currencies", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var currenciesResponse = JsonSerializer.Deserialize<CurrenciesResponse>(content, _jsonOptions);
            var currencies = currenciesResponse?.Currencies ?? Enumerable.Empty<CurrencyDto>();

            _logger.LogInformation("Successfully retrieved {CurrencyCount} supported currencies", currencies.Count());
            return currencies;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting supported currencies");
            throw new ExternalServiceException($"Failed to get supported currencies: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting supported currencies");
            throw new ExternalServiceException("Timeout while getting supported currencies", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while getting supported currencies");
            throw new InvalidDataException("JSON parsing error while getting supported currencies", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CurrencyDto?> ValidateCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                throw new ArgumentException("Currency code cannot be null or empty", nameof(currencyCode));
            }

            _logger.LogInformation("Validating currency code: {CurrencyCode}", currencyCode);

            var response = await _httpClient.GetAsync($"/validate/{currencyCode}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Currency code not found: {CurrencyCode}", currencyCode);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var validationResponse = JsonSerializer.Deserialize<CurrencyValidationDto>(content, _jsonOptions);

            _logger.LogInformation("Currency validation successful for {CurrencyCode}", currencyCode);
            return validationResponse?.CurrencyInfo;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while validating currency {CurrencyCode}", currencyCode);
            throw new ExternalServiceException($"Failed to validate currency {currencyCode}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while validating currency {CurrencyCode}", currencyCode);
            throw new ExternalServiceException($"Timeout while validating currency {currencyCode}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while validating currency {CurrencyCode}", currencyCode);
            throw new ExternalServiceException($"Invalid response format while validating currency {currencyCode}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HistoricalExchangeRateDto>> GetHistoricalExchangeRatesAsync(
        string fromCurrency,
        string toCurrency,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                throw new ArgumentException("Currency codes cannot be null or empty");
            }

            if (startDate > endDate)
            {
                throw new ArgumentException("Start date cannot be greater than end date");
            }

            _logger.LogInformation("Getting historical exchange rates from {FromCurrency} to {ToCurrency} from {StartDate} to {EndDate}",
                fromCurrency, toCurrency, startDate, endDate);

            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            var response = await _httpClient.GetAsync(
                $"/historical/{fromCurrency}/{toCurrency}?startDate={startDateStr}&endDate={endDateStr}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Historical exchange rates not found for {FromCurrency} to {ToCurrency}",
                    fromCurrency, toCurrency);
                return Enumerable.Empty<HistoricalExchangeRateDto>();
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var historicalResponse = JsonSerializer.Deserialize<HistoricalExchangeRatesResponse>(content, _jsonOptions);
            var historicalRates = historicalResponse?.Rates ?? Enumerable.Empty<HistoricalExchangeRateDto>();

            // Populate FromCurrency and ToCurrency for each rate if not already set
            var populatedRates = historicalRates.Select(rate =>
            {
                if (string.IsNullOrEmpty(rate.FromCurrency))
                    rate.FromCurrency = fromCurrency;
                if (string.IsNullOrEmpty(rate.ToCurrency))
                    rate.ToCurrency = toCurrency;
                return rate;
            });

            _logger.LogInformation("Successfully retrieved {RateCount} historical exchange rates for {FromCurrency} to {ToCurrency}",
                populatedRates.Count(), fromCurrency, toCurrency);
            return populatedRates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting historical exchange rates {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Failed to get historical exchange rates {fromCurrency} to {toCurrency}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting historical exchange rates {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Timeout while getting historical exchange rates {fromCurrency} to {toCurrency}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting historical exchange rates {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Invalid response format while getting historical exchange rates {fromCurrency} to {toCurrency}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CurrencyDto?> GetCurrencyInfoAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                throw new ArgumentException("Currency code cannot be null or empty", nameof(currencyCode));
            }

            _logger.LogInformation("Getting currency information for: {CurrencyCode}", currencyCode);

            var response = await _httpClient.GetAsync($"/currencies/{currencyCode}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Currency information not found for: {CurrencyCode}", currencyCode);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var currency = JsonSerializer.Deserialize<CurrencyDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved currency information for: {CurrencyCode}", currencyCode);
            return currency;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting currency info {CurrencyCode}", currencyCode);
            throw new ExternalServiceException($"Failed to get currency info {currencyCode}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting currency info {CurrencyCode}", currencyCode);
            throw new ExternalServiceException($"Timeout while getting currency info {currencyCode}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting currency info {CurrencyCode}", currencyCode);
            throw new ExternalServiceException($"Invalid response format while getting currency info {currencyCode}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CurrencyConversionResult> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            if (amount < 0)
            {
                throw new ArgumentException("Amount cannot be negative", nameof(amount));
            }

            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                throw new ArgumentException("Currency codes cannot be null or empty");
            }

            _logger.LogInformation("Converting {Amount} from {FromCurrency} to {ToCurrency}",
                amount, fromCurrency, toCurrency);

            var response = await _httpClient.GetAsync(
                $"/currency-conversion?amount={amount}&from={fromCurrency}&to={toCurrency}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var conversion = JsonSerializer.Deserialize<CurrencyConversionResult>(content, _jsonOptions);

            _logger.LogInformation("Successfully converted {Amount} {FromCurrency} to {ConvertedAmount} {ToCurrency}",
                amount, fromCurrency, conversion?.ConvertedAmount, toCurrency);
            return conversion!;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while converting currency {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Failed to convert currency {fromCurrency} to {toCurrency}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while converting currency {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Timeout while converting currency {fromCurrency} to {toCurrency}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while converting currency {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            throw new ExternalServiceException($"Invalid response format while converting currency {fromCurrency} to {toCurrency}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateCurrencyCodeAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ValidateCurrencyAsync(currencyCode, cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating currency code {CurrencyCode}", currencyCode);
            return false;
        }
    }
}