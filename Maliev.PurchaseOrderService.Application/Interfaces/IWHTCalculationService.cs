namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface IWHTCalculationService
{
    decimal CalculateWHT(decimal amount, decimal rate);
}
