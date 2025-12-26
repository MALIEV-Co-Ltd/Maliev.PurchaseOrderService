namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for calculating withholding tax (WHT) according to Thailand tax regulations
/// </summary>
public interface IWHTCalculationService
{
    /// <summary>
    /// Calculates the Withholding Tax (WHT) amount based on the subtotal and WHT rate.
    /// </summary>
    /// <param name="subtotal">The subtotal amount before WHT calculation.</param>
    /// <param name="whtRate">The WHT rate percentage (e.g., 3 for 3%).</param>
    /// <returns>The calculated WHT amount, rounded to two decimal places.</returns>
    decimal CalculateWHT(decimal subtotal, decimal? whtRate);
}
