namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface ICurrencyServiceClient
{
    Task<CurrencyDto?> GetCurrencyAsync(int currencyId, CancellationToken cancellationToken = default);
    Task<bool> ValidateCurrencyExistsAsync(int currencyId, CancellationToken cancellationToken = default);
}

public class CurrencyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
}
