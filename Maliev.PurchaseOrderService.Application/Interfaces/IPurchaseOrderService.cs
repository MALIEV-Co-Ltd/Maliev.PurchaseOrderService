using Maliev.PurchaseOrderService.Application.DTOs;
using Maliev.PurchaseOrderService.Domain.Entities;

namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDetailResponse> CreateAsync(CreatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailResponse?> GetByIdAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<PurchaseOrderResponse>> SearchAsync(SearchPurchaseOrdersRequest request, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailResponse> UpdateAsync(int id, UpdatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailResponse> ApproveAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailResponse> CancelAsync(int id, string reason, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailResponse> SendToSupplierAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailResponse> ReceiveGoodsAsync(int id, bool isPartialReceipt, string userId, string userRole, CancellationToken cancellationToken = default);
    Task<PurchaseOrderFileResponse> RegisterFileAsync(int id, RegisterPurchaseOrderFileRequest request, string userId, string userRole, CancellationToken cancellationToken = default);
}
