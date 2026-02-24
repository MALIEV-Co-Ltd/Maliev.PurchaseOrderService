namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Client for interacting with CurrencyService
/// </summary>
public interface ICurrencyServiceClient
{
    /// <summary>
    /// Retrieves a currency by its ID asynchronously.
    /// </summary>
    /// <param name="currencyId">The ID of the currency to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="CurrencyDto"/> if found, otherwise null.</returns>
    Task<CurrencyDto?> GetCurrencyAsync(int currencyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a currency with the specified ID exists asynchronously.
    /// </summary>
    /// <param name="currencyId">The ID of the currency to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the currency exists, false otherwise.</returns>
    Task<bool> ValidateCurrencyExistsAsync(int currencyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for currency information.
/// </summary>
public class CurrencyDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the currency.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., "USD", "THB").
    /// </summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the symbol used for the currency (e.g., "$", "฿").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the full name of the currency (e.g., "United States Dollar").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the exchange rate relative to a base currency.
    /// </summary>
    public decimal ExchangeRate { get; set; }
}
