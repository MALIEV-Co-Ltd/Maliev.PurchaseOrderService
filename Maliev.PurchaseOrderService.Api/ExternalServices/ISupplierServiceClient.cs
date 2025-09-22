using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Interface for Supplier Service external API client
/// </summary>
public interface ISupplierServiceClient
{
    /// <summary>
    /// Gets supplier information by ID
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier information or null if not found</returns>
    Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supplier contact information by ID
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier contact information or null if not found</returns>
    Task<SupplierContactDto?> GetSupplierContactAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a supplier exists and is active, returning supplier information
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier information if valid and active, null otherwise</returns>
    Task<SupplierDto?> ValidateSupplierAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supplier product catalog
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supplier products</returns>
    Task<IEnumerable<SupplierProductDto>> GetSupplierProductsAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supplier payment terms
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier payment terms or null if not found</returns>
    Task<SupplierPaymentTermsDto?> GetSupplierPaymentTermsAsync(int supplierId, CancellationToken cancellationToken = default);
}