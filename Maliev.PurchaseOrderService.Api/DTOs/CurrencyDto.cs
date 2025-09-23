namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Data Transfer Object for Currency information
/// </summary>
public class CurrencyDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data Transfer Object for Exchange Rate information
/// </summary>
public class ExchangeRateDto
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal InverseRate { get; set; }
    public DateTime RateDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsRealTime { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal Spread { get; set; }
    public decimal BidRate { get; set; }
    public decimal AskRate { get; set; }
}

/// <summary>
/// Data Transfer Object for Currency Conversion result
/// </summary>
public class CurrencyConversionDto
{
    public decimal OriginalAmount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string ToCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime ConversionDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsEstimate { get; set; }
    public DateTime ValidUntil { get; set; }
}

/// <summary>
/// Data Transfer Object for Historical Exchange Rate information
/// </summary>
public class HistoricalExchangeRateDto
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
    public decimal OpenRate { get; set; }
    public decimal HighRate { get; set; }
    public decimal LowRate { get; set; }
    public decimal CloseRate { get; set; }
    public decimal Volume { get; set; }
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for Currency validation result
/// </summary>
public class CurrencyValidationDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsSupported { get; set; }
    public bool IsActive { get; set; }
    public string Message { get; set; } = string.Empty;
    public CurrencyDto? CurrencyInfo { get; set; }
}

/// <summary>
/// Wrapper DTO for currencies list response
/// </summary>
public class CurrenciesResponse
{
    public IEnumerable<CurrencyDto> Currencies { get; set; } = new List<CurrencyDto>();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Wrapper DTO for historical exchange rates response
/// </summary>
public class HistoricalExchangeRatesResponse
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IEnumerable<HistoricalExchangeRateDto> Rates { get; set; } = new List<HistoricalExchangeRateDto>();
}