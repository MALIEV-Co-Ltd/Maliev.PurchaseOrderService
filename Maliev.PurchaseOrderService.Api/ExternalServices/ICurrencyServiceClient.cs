using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Interface for Currency Service external API client
/// </summary>
public interface ICurrencyServiceClient
{
    /// <summary>
    /// Gets current exchange rate between two currencies
    /// </summary>
    /// <param name="fromCurrency">Source currency code (e.g., USD)</param>
    /// <param name="toCurrency">Target currency code (e.g., THB)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate information or null if not found</returns>
    Task<ExchangeRateDto?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts amount from one currency to another
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency conversion result</returns>
    Task<CurrencyConversionDto?> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of supported currencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supported currencies</returns>
    Task<IEnumerable<CurrencyDto>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a currency code is supported and returns currency information
    /// </summary>
    /// <param name="currencyCode">Currency code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency information if supported, null otherwise</returns>
    Task<CurrencyDto?> ValidateCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical exchange rates for a currency pair
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="startDate">Start date for historical data</param>
    /// <param name="endDate">End date for historical data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical exchange rates</returns>
    Task<IEnumerable<HistoricalExchangeRateDto>> GetHistoricalExchangeRatesAsync(
        string fromCurrency,
        string toCurrency,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets currency information by code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency information or null if not found</returns>
    Task<CurrencyDto?> GetCurrencyInfoAsync(string currencyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts currency using external service (alternative signature for tests)
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="amount">Amount to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency conversion result</returns>
    Task<CurrencyConversionResult> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount, CancellationToken cancellationToken = default);
}