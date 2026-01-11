namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Implementation of WHT calculation service
/// </summary>
public class WHTCalculationService : IWHTCalculationService
{
    /// <inheritdoc/>
    public decimal CalculateWHT(decimal subtotal, decimal? whtRate)
    {
        if (!whtRate.HasValue || whtRate.Value <= 0)
        {
            return 0m;
        }

        // Calculate WHT amount: subtotal * (rate / 100)
        var whtAmount = subtotal * (whtRate.Value / 100m);

        // Round to 2 decimal places using commercial rounding
        return Math.Round(whtAmount, 2, MidpointRounding.AwayFromZero);
    }
}
