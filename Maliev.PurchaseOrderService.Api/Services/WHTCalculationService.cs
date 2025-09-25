using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for calculating withholding tax according to Thailand tax regulations
/// </summary>
public class WHTCalculationService : IWHTCalculationService
{
    private readonly ICurrencyServiceClient _currencyService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WHTCalculationService> _logger;

    // Thailand WHT rates according to current tax law
    private readonly Dictionary<string, decimal> _whtRates = new()
    {
        // Services by Thai residents
        ["thai_professional_services"] = 0.03m,      // 3% - Professional services
        ["thai_technical_services"] = 0.03m,         // 3% - Technical services
        ["thai_rental_movable"] = 0.05m,             // 5% - Rental of movable property
        ["thai_rental_immovable"] = 0.05m,           // 5% - Rental of immovable property
        ["thai_construction"] = 0.03m,               // 3% - Construction services
        ["thai_transportation"] = 0.01m,             // 1% - Transportation services
        ["thai_advertising"] = 0.02m,                // 2% - Advertising services
        ["thai_goods"] = 0.01m,                      // 1% - Sale of goods to government

        // Services by non-residents
        ["non_resident_services"] = 0.03m,           // 3% - General services
        ["non_resident_technical"] = 0.15m,          // 15% - Technical services
        ["non_resident_management"] = 0.15m,         // 15% - Management fees
        ["non_resident_royalty"] = 0.15m,            // 15% - Royalties
        ["non_resident_interest"] = 0.15m,           // 15% - Interest payments
        ["non_resident_dividend"] = 0.10m,           // 10% - Dividends
        ["non_resident_rental"] = 0.15m,             // 15% - Rental income

        // Default rates
        ["default_thai"] = 0.03m,                    // 3% - Default for Thai residents
        ["default_non_resident"] = 0.15m             // 15% - Default for non-residents
    };

    // Minimum threshold amounts for WHT (in THB)
    private readonly Dictionary<string, decimal> _whtThresholds = new()
    {
        ["professional_services"] = 1000m,
        ["technical_services"] = 1000m,
        ["rental"] = 1000m,
        ["construction"] = 1000m,
        ["transportation"] = 1000m,
        ["advertising"] = 1000m,
        ["goods"] = 1000m,
        ["default"] = 1000m
    };

    public WHTCalculationService(
        ICurrencyServiceClient currencyService,
        IMemoryCache cache,
        ILogger<WHTCalculationService> logger)
    {
        _currencyService = currencyService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WHTCalculationResult> CalculateWHTAsync(
        SupplierDto supplierDto,
        decimal subtotalAmount,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating WHT for supplier {SupplierId}, amount {Amount} {Currency}",
            supplierDto.Id, subtotalAmount, currencyCode);

        try
        {
            var result = new WHTCalculationResult
            {
                SubtotalAmount = subtotalAmount,
                CurrencyCode = currencyCode,
                NetAmount = subtotalAmount
            };

            // Check if WHT is applicable
            if (!IsWHTApplicable(supplierDto, subtotalAmount, currencyCode))
            {
                result.IsApplicable = false;
                result.Reason = GetNonApplicableReason(supplierDto, subtotalAmount, currencyCode);
                result.TaxRegulation = GetTaxRegulation(supplierDto);
                _logger.LogInformation("WHT not applicable for supplier {SupplierId}: {Reason}",
                    supplierDto.Id, result.Reason);
                return result;
            }

            // Get WHT rate
            var whtRate = GetWHTRate(supplierDto.SupplierType, supplierDto.ServiceCategory, supplierDto.IsThaiResident);
            result.WHTRate = whtRate;

            // Handle zero rate case
            if (whtRate == 0m)
            {
                result.IsApplicable = false;
                result.WHTAmount = 0m;
                result.NetAmount = subtotalAmount;
                result.Reason = "WHT rate is zero for this supplier type";
                result.TaxRegulation = GetTaxRegulation(supplierDto);
                return result;
            }

            // Calculate WHT amount
            result.WHTAmount = Math.Round(subtotalAmount * whtRate, 2, MidpointRounding.AwayFromZero);
            result.NetAmount = subtotalAmount - result.WHTAmount;

            // Convert to THB for tax reporting
            result.WHTAmountTHB = await ConvertToTHBAsync(result.WHTAmount, currencyCode, cancellationToken);

            // Get exchange rate if conversion was needed
            if (currencyCode != "THB")
            {
                var exchangeRateDto = await _currencyService.GetExchangeRateAsync("THB", currencyCode, cancellationToken);
                result.ExchangeRate = exchangeRateDto?.Rate;
            }

            result.IsApplicable = true;
            result.Reason = $"WHT applied at {whtRate:P2} rate for company supplier";

            // Generate WHT certificate number
            result.WHTCertificateNumber = GenerateWHTCertificateNumber();

            _logger.LogInformation("WHT calculated: {WHTAmount} {Currency} ({WHTAmountTHB} THB) at {Rate:P2}",
                result.WHTAmount, currencyCode, result.WHTAmountTHB, whtRate);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating WHT for supplier {SupplierId}", supplierDto.Id);
            throw;
        }
    }

    public decimal GetWHTRate(string supplierType, string serviceCategory, bool isThaiResident)
    {
        var key = BuildWHTRateKey(supplierType, serviceCategory, isThaiResident);

        if (_whtRates.TryGetValue(key, out var rate))
        {
            return rate;
        }

        // Fallback to default rates
        return isThaiResident ? _whtRates["default_thai"] : _whtRates["default_non_resident"];
    }

    public bool IsWHTApplicable(SupplierDto supplierDto, decimal subtotalAmount, string currencyCode)
    {
        // WHT is not applicable for certain cases
        if (supplierDto.IsWHTExempt)
        {
            return false;
        }

        // Convert amount to THB for threshold checking
        var amountInTHB = currencyCode == "THB" ? subtotalAmount : ConvertToTHBAsync(subtotalAmount, currencyCode).Result;

        // Check minimum threshold
        var threshold = GetWHTThreshold(supplierDto.ServiceCategory);
        if (amountInTHB < threshold)
        {
            return false;
        }

        // Special case: Foreign suppliers (non-Thai residents) are typically NOT subject to WHT
        // as they are governed by different tax treaties and withholding arrangements
        if (!supplierDto.IsThaiResident)
        {
            return false; // Foreign suppliers are exempt from Thai WHT
        }

        // WHT generally applies to services and certain goods for Thai residents
        var applicableCategories = new[]
        {
            "professional_services", "technical_services", "construction",
            "rental", "transportation", "advertising", "management",
            "royalty", "interest", "dividend", "services", "goods"
        };

        return applicableCategories.Contains(supplierDto.ServiceCategory);
    }

    public async Task<decimal> ConvertToTHBAsync(decimal amount, string fromCurrency, CancellationToken cancellationToken = default)
    {
        if (fromCurrency == "THB")
        {
            return amount;
        }

        var cacheKey = $"exchange_rate_{fromCurrency}_THB";

        if (!_cache.TryGetValue(cacheKey, out decimal exchangeRate))
        {
            var exchangeRateDto = await _currencyService.GetExchangeRateAsync("THB", fromCurrency, cancellationToken);
            exchangeRate = exchangeRateDto?.Rate ?? 1m;

            // Cache for 1 hour
            _cache.Set(cacheKey, exchangeRate, TimeSpan.FromHours(1));
        }

        return Math.Round(amount * exchangeRate, 2, MidpointRounding.AwayFromZero);
    }

    private string BuildWHTRateKey(string supplierType, string serviceCategory, bool isThaiResident)
    {
        var prefix = isThaiResident ? "thai" : "non_resident";
        var category = serviceCategory.ToLowerInvariant().Replace(" ", "_");

        return $"{prefix}_{category}";
    }

    private decimal GetWHTThreshold(string serviceCategory)
    {
        var category = serviceCategory.ToLowerInvariant().Replace(" ", "_");
        return _whtThresholds.TryGetValue(category, out var threshold) ? threshold : _whtThresholds["default"];
    }

    private string GetNonApplicableReason(SupplierDto supplierDto, decimal subtotalAmount, string currencyCode)
    {
        if (supplierDto.IsWHTExempt)
        {
            return "Supplier is exempt from withholding tax";
        }

        if (!supplierDto.IsThaiResident)
        {
            return "Not applicable for foreign suppliers";
        }

        var amountInTHB = currencyCode == "THB" ? subtotalAmount : ConvertToTHBAsync(subtotalAmount, currencyCode).Result;
        var threshold = GetWHTThreshold(supplierDto.ServiceCategory);

        if (amountInTHB < threshold)
        {
            return $"Amount {amountInTHB:N2} THB is below minimum threshold of {threshold:N2} THB";
        }

        return "Transaction type not subject to withholding tax";
    }

    private string GetTaxRegulation(SupplierDto supplierDto)
    {
        if (!supplierDto.IsThaiResident)
        {
            return "Not applicable for foreign suppliers";
        }

        if (supplierDto.IsWHTExempt)
        {
            return "Exempt from withholding tax";
        }

        return "Thailand Revenue Code Section 3";
    }

    private static string GenerateWHTCertificateNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"WHT-{timestamp}-{random}";
    }
}