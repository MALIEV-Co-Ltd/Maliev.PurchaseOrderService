using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Implementation of CurrencyService client with 1-hour caching (NFR-001)
/// </summary>
public class CurrencyServiceClient : ICurrencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyServiceClient> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyServiceClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public CurrencyServiceClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<CurrencyServiceClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CurrencyService");
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CurrencyDto?> GetCurrencyAsync(int currencyId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"currency_{currencyId}";

        if (_cache.TryGetValue(cacheKey, out CurrencyDto? cachedCurrency))
        {
            _logger.LogDebug("Currency {CurrencyId} fetched from cache", currencyId);
            return cachedCurrency;
        }

        try
        {
            // Base URL already includes /currencies/v1, so just request /{id}
            var response = await _httpClient.GetAsync($"{currencyId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch currency {CurrencyId}: {StatusCode}", currencyId, response.StatusCode);
                return null;
            }

            var currency = await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken);

            if (currency != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration,
                    Size = 1 // Each currency entry counts as 1 unit
                };
                _cache.Set(cacheKey, currency, cacheOptions);
                _logger.LogDebug("Currency {CurrencyId} cached for {Duration}", currencyId, _cacheDuration);
            }

            return currency;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency {CurrencyId}", currencyId);
            throw new ExternalServiceException($"Failed to fetch currency {currencyId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateCurrencyExistsAsync(int currencyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currency = await GetCurrencyAsync(currencyId, cancellationToken);
            return currency != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating currency {CurrencyId}", currencyId);
            return false;
        }
    }
}
