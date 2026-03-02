using Maliev.PurchaseOrderService.Domain.Entities;
namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Client for interacting with SupplierService
/// </summary>
public interface ISupplierServiceClient
{
    /// <summary>
    /// Retrieves a supplier by its ID asynchronously.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="SupplierDto"/> if found, otherwise null.</returns>
    Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a supplier with the specified ID exists asynchronously.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the supplier exists, false otherwise.</returns>
    Task<bool> ValidateSupplierExistsAsync(int supplierId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for supplier information.
/// </summary>
public class SupplierDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the supplier.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the supplier.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contact information for the supplier.
    /// </summary>
    public string ContactInfo { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the email address of the supplier.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the phone number of the supplier.
    /// </summary>
    public string Phone { get; set; } = string.Empty;
}
