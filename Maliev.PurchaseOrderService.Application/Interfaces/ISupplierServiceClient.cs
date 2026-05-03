namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface ISupplierServiceClient
{
    Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<bool> ValidateSupplierExistsAsync(int supplierId, CancellationToken cancellationToken = default);
}

public class SupplierDto
{
    public int Id { get; set; }
    public Guid? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
