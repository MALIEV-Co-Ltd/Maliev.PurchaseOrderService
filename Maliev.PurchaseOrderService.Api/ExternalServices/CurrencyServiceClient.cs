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

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _logger.LogInformation("Fetching currency {CurrencyId} from external service", currencyId);
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            entry.Size = 1;

            try
            {
                var response = await _httpClient.GetAsync($"{currencyId}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch currency {CurrencyId}: {StatusCode}", currencyId, response.StatusCode);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currency {CurrencyId}", currencyId);
                throw new ExternalServiceException($"Failed to fetch currency {currencyId}", ex);
            }
        });
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
