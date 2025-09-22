using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Interface for withholding tax calculation service following Thailand tax regulations
/// </summary>
public interface IWHTCalculationService
{
    /// <summary>
    /// Calculates withholding tax for a purchase order based on Thailand regulations
    /// </summary>
    /// <param name="supplierDto">Supplier information</param>
    /// <param name="subtotalAmount">Subtotal amount before WHT</param>
    /// <param name="currencyCode">Currency code (e.g., "THB", "USD")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WHT calculation result</returns>
    Task<WHTCalculationResult> CalculateWHTAsync(
        SupplierDto supplierDto,
        decimal subtotalAmount,
        string currencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets applicable WHT rate for a supplier category
    /// </summary>
    /// <param name="supplierType">Type of supplier</param>
    /// <param name="serviceCategory">Category of service/goods</param>
    /// <param name="isThaiResident">Whether supplier is Thai tax resident</param>
    /// <returns>WHT rate as decimal (e.g., 0.03 for 3%)</returns>
    decimal GetWHTRate(string supplierType, string serviceCategory, bool isThaiResident);

    /// <summary>
    /// Validates if WHT is applicable for the transaction
    /// </summary>
    /// <param name="supplierDto">Supplier information</param>
    /// <param name="subtotalAmount">Subtotal amount</param>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>True if WHT should be applied</returns>
    bool IsWHTApplicable(SupplierDto supplierDto, decimal subtotalAmount, string currencyCode);

    /// <summary>
    /// Converts amount to THB for WHT calculation if needed
    /// </summary>
    /// <param name="amount">Amount in original currency</param>
    /// <param name="fromCurrency">Original currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Amount in THB</returns>
    Task<decimal> ConvertToTHBAsync(decimal amount, string fromCurrency, CancellationToken cancellationToken = default);
}