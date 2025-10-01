using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Currency API Controller for managing currency operations
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/api/currencies")]
[ApiVersion("1.0")]
[ApiVersion("1")]
[Authorize]
[Produces("application/json")]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyServiceClient _currencyService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrenciesController> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    public CurrenciesController(
        ICurrencyServiceClient currencyService,
        IMemoryCache cache,
        ILogger<CurrenciesController> logger)
    {
        _currencyService = currencyService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets list of supported currencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supported currencies</returns>
    [HttpGet]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(IEnumerable<CurrencyDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<CurrencyDto>>> GetSupportedCurrencies(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting supported currencies");

            const string cacheKey = "supported_currencies";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<CurrencyDto>? cachedCurrencies))
            {
                _logger.LogDebug("Returning cached currencies");
                return Ok(cachedCurrencies);
            }

            var currencies = await _currencyService.GetSupportedCurrenciesAsync(cancellationToken);

            // Cache the result
            _cache.Set(cacheKey, currencies, CacheExpiration);

            return Ok(currencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported currencies");
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving supported currencies",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets current exchange rates for multiple currency pairs
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrencies">Target currency codes (comma-separated)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of exchange rates</returns>
    [HttpGet("exchange-rates")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetExchangeRates(
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrencies,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrencies))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Both fromCurrency and toCurrencies are required",
                        Code = "MISSING_CURRENCY_CODES"
                    }
                });
            }

            _logger.LogInformation("Getting exchange rates from {FromCurrency} to {ToCurrencies}", fromCurrency, toCurrencies);

            var targetCurrencies = toCurrencies.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(c => c.Trim())
                                              .ToArray();

            var exchangeRates = new Dictionary<string, decimal>();

            foreach (var toCurrency in targetCurrencies)
            {
                var cacheKey = $"exchange_rate_{fromCurrency}_{toCurrency}";
                decimal rate = 1.0m; // Default rate

                if (_cache.TryGetValue(cacheKey, out ExchangeRateDto? cachedRate) && cachedRate != null)
                {
                    rate = cachedRate.Rate;
                }
                else
                {
                    var exchangeRate = await _currencyService.GetExchangeRateAsync(fromCurrency, toCurrency, cancellationToken);
                    if (exchangeRate != null)
                    {
                        rate = exchangeRate.Rate;
                        _cache.Set(cacheKey, exchangeRate, TimeSpan.FromMinutes(15));
                    }
                }

                exchangeRates[toCurrency] = rate;
            }

            return Ok(exchangeRates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rates from {FromCurrency} to {ToCurrencies}", fromCurrency, toCurrencies);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving exchange rates",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets exchange rate between two currencies
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate information</returns>
    [HttpGet("exchange-rate")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(ExchangeRateDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ExchangeRateDto>> GetExchangeRate(
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Both fromCurrency and toCurrency are required",
                        Code = "MISSING_CURRENCY_CODES"
                    }
                });
            }

            _logger.LogInformation("Getting exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);

            var cacheKey = $"exchange_rate_{fromCurrency}_{toCurrency}";
            if (_cache.TryGetValue(cacheKey, out ExchangeRateDto? cachedRate))
            {
                _logger.LogDebug("Returning cached exchange rate");
                return Ok(cachedRate);
            }

            var exchangeRate = await _currencyService.GetExchangeRateAsync(fromCurrency, toCurrency, cancellationToken);

            if (exchangeRate == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Exchange rate not found for {fromCurrency} to {toCurrency}",
                        Code = "EXCHANGE_RATE_NOT_FOUND"
                    }
                });
            }

            // Cache the result for a shorter time (exchange rates change frequently)
            _cache.Set(cacheKey, exchangeRate, TimeSpan.FromMinutes(15));

            return Ok(exchangeRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving exchange rate",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Converts amount from one currency to another
    /// </summary>
    /// <param name="request">Currency conversion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency conversion result</returns>
    [HttpPost("convert")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(CurrencyConversionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<CurrencyConversionDto>> ConvertCurrency(
        [FromBody] CurrencyConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid conversion request",
                        Code = "INVALID_REQUEST"
                    }
                });
            }

            _logger.LogInformation("Converting {Amount} from {FromCurrency} to {ToCurrency}",
                request.Amount, request.FromCurrency, request.ToCurrency);

            var conversion = await _currencyService.ConvertCurrencyAsync(
                request.FromCurrency, request.ToCurrency, request.Amount, cancellationToken);

            if (conversion == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Currency conversion failed",
                        Code = "CONVERSION_FAILED"
                    }
                });
            }

            return Ok(conversion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency from {FromCurrency} to {ToCurrency}",
                request.FromCurrency, request.ToCurrency);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while converting currency",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Validates a currency code
    /// </summary>
    /// <param name="currencyCode">Currency code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency validation result</returns>
    [HttpGet("validate/{currencyCode}")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(CurrencyValidationDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<CurrencyValidationDto>> ValidateCurrency(
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Currency code is required",
                        Code = "MISSING_CURRENCY_CODE"
                    }
                });
            }

            _logger.LogInformation("Validating currency code: {CurrencyCode}", currencyCode);

            var isValid = await _currencyService.ValidateCurrencyCodeAsync(currencyCode, cancellationToken);
            var currencyInfo = isValid ? await _currencyService.ValidateCurrencyAsync(currencyCode, cancellationToken) : null;

            var validation = new CurrencyValidationDto
            {
                CurrencyCode = currencyCode,
                IsValid = isValid,
                IsSupported = isValid,
                IsActive = isValid,
                Message = isValid ? "Currency is valid and supported" : "Currency is not supported",
                CurrencyInfo = currencyInfo
            };

            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating currency code: {CurrencyCode}", currencyCode);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while validating currency",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets historical exchange rates
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical exchange rates</returns>
    [HttpGet("history")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(IEnumerable<HistoricalExchangeRateDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<HistoricalExchangeRateDto>>> GetHistoricalExchangeRates(
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrency,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Both fromCurrency and toCurrency are required",
                        Code = "MISSING_CURRENCY_CODES"
                    }
                });
            }

            if (startDate >= endDate)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Start date must be before end date",
                        Code = "INVALID_DATE_RANGE"
                    }
                });
            }

            _logger.LogInformation("Getting historical exchange rates from {FromCurrency} to {ToCurrency} between {StartDate} and {EndDate}",
                fromCurrency, toCurrency, startDate, endDate);

            var rates = await _currencyService.GetHistoricalExchangeRatesAsync(
                fromCurrency, toCurrency, startDate, endDate, cancellationToken);

            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical exchange rates from {FromCurrency} to {ToCurrency}",
                fromCurrency, toCurrency);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving historical exchange rates",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Refreshes the currency cache by clearing cached data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache refresh result</returns>
    [HttpPost("refresh-cache")]
    [Authorize(Roles = "Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<string>> RefreshCurrencyCache(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refreshing currency cache");

            // Clear all currency-related cache entries
            var cacheKeysToRemove = new[]
            {
                "supported_currencies"
            };

            foreach (var key in cacheKeysToRemove)
            {
                _cache.Remove(key);
            }

            // Preload currencies to warm up the cache
            await _currencyService.GetSupportedCurrenciesAsync(cancellationToken);

            return Ok("Cache refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing currency cache");
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while refreshing currency cache",
                    Code = "CACHE_REFRESH_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets popular currencies based on purchase order usage statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of popular currencies with usage statistics</returns>
    [HttpGet("popular")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(IEnumerable<CurrencyUsageStatistics>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<ActionResult<IEnumerable<CurrencyUsageStatistics>>> GetPopularCurrencies(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting popular currencies statistics");

            const string cacheKey = "popular_currencies";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<CurrencyUsageStatistics>? cachedStats))
            {
                _logger.LogDebug("Returning cached popular currencies");
                return Task.FromResult<ActionResult<IEnumerable<CurrencyUsageStatistics>>>(Ok(cachedStats));
            }

            // For now, return mock data as this would typically query the database
            // In a real implementation, this would query purchase orders and aggregate currency usage
            var popularCurrencies = new List<CurrencyUsageStatistics>
            {
                new()
                {
                    CurrencyCode = "USD",
                    CurrencyName = "US Dollar",
                    UsageCount = 2,
                    TotalValue = 2000.00m,
                    UsagePercentage = 50.0m,
                    AverageOrderValue = 1000.00m,
                    LastUsedAt = DateTime.UtcNow,
                    Trend = "stable"
                },
                new()
                {
                    CurrencyCode = "THB",
                    CurrencyName = "Thai Baht",
                    UsageCount = 1,
                    TotalValue = 35000.00m,
                    UsagePercentage = 25.0m,
                    AverageOrderValue = 35000.00m,
                    LastUsedAt = DateTime.UtcNow.AddHours(-1),
                    Trend = "increasing"
                },
                new()
                {
                    CurrencyCode = "EUR",
                    CurrencyName = "Euro",
                    UsageCount = 1,
                    TotalValue = 920.00m,
                    UsagePercentage = 25.0m,
                    AverageOrderValue = 920.00m,
                    LastUsedAt = DateTime.UtcNow.AddHours(-2),
                    Trend = "stable"
                }
            };

            // Cache the result
            _cache.Set(cacheKey, popularCurrencies, TimeSpan.FromMinutes(10));

            return Task.FromResult<ActionResult<IEnumerable<CurrencyUsageStatistics>>>(Ok(popularCurrencies));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular currencies statistics");
            return Task.FromResult<ActionResult<IEnumerable<CurrencyUsageStatistics>>>(StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving popular currencies",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            }));
        }
    }

    /// <summary>
    /// Validates a specific currency code and returns validation information
    /// </summary>
    /// <param name="currencyCode">Currency code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency validation result</returns>
    [HttpGet("{currencyCode}/validate")]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(CurrencyDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<CurrencyDto>> ValidateCurrencyCode(
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Currency code is required",
                        Code = "MISSING_CURRENCY_CODE"
                    }
                });
            }

            _logger.LogInformation("Validating currency code: {CurrencyCode}", currencyCode);

            var cacheKey = $"currency_validation_{currencyCode}";
            if (_cache.TryGetValue(cacheKey, out CurrencyDto? cachedCurrency))
            {
                _logger.LogDebug("Returning cached currency validation for {CurrencyCode}", currencyCode);
                return Ok(cachedCurrency);
            }

            var currency = await _currencyService.ValidateCurrencyAsync(currencyCode, cancellationToken);

            if (currency == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Currency {currencyCode} is not supported",
                        Code = "CURRENCY_NOT_SUPPORTED"
                    }
                });
            }

            // Cache the result
            _cache.Set(cacheKey, currency, CacheExpiration);

            return Ok(currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating currency code: {CurrencyCode}", currencyCode);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while validating currency",
                    Code = "CURRENCY_SERVICE_ERROR"
                }
            });
        }
    }
}