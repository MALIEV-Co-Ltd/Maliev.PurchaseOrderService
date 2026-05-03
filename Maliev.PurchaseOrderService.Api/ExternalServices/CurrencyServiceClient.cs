using Maliev.PurchaseOrderService.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

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
    public async Task<CurrencyDto?> GetCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"currency_{currencyId:D}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _logger.LogInformation("Fetching currency {CurrencyId} from external service", currencyId);
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            entry.Size = 1;

            try
            {
                var response = await _httpClient.GetAsync($"{currencyId:D}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch currency {CurrencyId}: {StatusCode}", currencyId, response.StatusCode);
                    return null;
                }

                var currency = await response.Content.ReadFromJsonAsync<CurrencyServiceCurrencyResponse>(cancellationToken);
                return currency?.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currency {CurrencyId}", currencyId);
                throw new ExternalServiceException($"Failed to fetch currency {currencyId}", ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task<CurrencyDto?> GetCurrencyByCodeAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = currencyCode.Trim().ToUpperInvariant();
        var cacheKey = $"currency_{normalizedCode}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _logger.LogInformation("Fetching currency {CurrencyCode} from external service", normalizedCode);
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            entry.Size = 1;

            try
            {
                var response = await _httpClient.GetAsync(Uri.EscapeDataString(normalizedCode), cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch currency {CurrencyCode}: {StatusCode}", normalizedCode, response.StatusCode);
                    return null;
                }

                var currency = await response.Content.ReadFromJsonAsync<CurrencyServiceCurrencyResponse>(cancellationToken);
                return currency?.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currency {CurrencyCode}", normalizedCode);
                throw new ExternalServiceException($"Failed to fetch currency {normalizedCode}", ex);
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

    private sealed record CurrencyServiceCurrencyResponse(
        JsonElement? Id,
        Guid? ExternalId,
        Guid? CurrencyId,
        string Code,
        string Symbol,
        string Name,
        decimal? ExchangeRate)
    {
        public CurrencyDto ToDto()
        {
            var (legacyId, externalId) = ParseId(Id);
            return new CurrencyDto
            {
                Id = legacyId,
                ExternalId = ExternalId ?? CurrencyId ?? externalId,
                Code = Code,
                Symbol = Symbol,
                Name = Name,
                ExchangeRate = ExchangeRate.GetValueOrDefault(1m)
            };
        }

        private static (int LegacyId, Guid? ExternalId) ParseId(JsonElement? id)
        {
            if (id is null)
            {
                return (0, null);
            }

            return id.Value.ValueKind switch
            {
                JsonValueKind.Number when id.Value.TryGetInt32(out var legacyId) => (legacyId, null),
                JsonValueKind.String when Guid.TryParse(id.Value.GetString(), out var externalId) => (0, externalId),
                _ => (0, null)
            };
        }
    }
}
