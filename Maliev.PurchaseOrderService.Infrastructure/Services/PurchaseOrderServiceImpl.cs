using Maliev.PurchaseOrderService.Application.DTOs;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.PurchaseOrderService.Infrastructure.Services;

public class PurchaseOrderServiceImpl : IPurchaseOrderService
{
    private readonly PurchaseOrderContext _context;
    private readonly ILogger<PurchaseOrderServiceImpl> _logger;

    public PurchaseOrderServiceImpl(PurchaseOrderContext context, ILogger<PurchaseOrderServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PurchaseOrderDetailResponse> CreateAsync(CreatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("CreateAsync not implemented yet");
    }

    public async Task<PurchaseOrderDetailResponse?> GetByIdAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .AsQueryable();

        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            return null;
        }

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PaginatedResponse<PurchaseOrderResponse>> SearchAsync(SearchPurchaseOrdersRequest request, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsNoTracking().AsQueryable();

        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PurchaseOrderResponse>(
            items.Select(MapToResponse).ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }

    public async Task<PurchaseOrderDetailResponse> UpdateAsync(int id, UpdatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("UpdateAsync not implemented yet");
    }

    public async Task<PurchaseOrderDetailResponse> ApproveAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ApproveAsync not implemented yet");
    }

    public async Task<PurchaseOrderDetailResponse> CancelAsync(int id, string reason, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("CancelAsync not implemented yet");
    }

    public async Task<PurchaseOrderDetailResponse> SendToSupplierAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SendToSupplierAsync not implemented yet");
    }

    public async Task<PurchaseOrderDetailResponse> ReceiveGoodsAsync(int id, bool isPartialReceipt, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ReceiveGoodsAsync not implemented yet");
    }

    private static PurchaseOrderDetailResponse MapToDetailResponse(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderDetailResponse
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            OrderType = purchaseOrder.OrderType.ToString(),
            Status = purchaseOrder.Status.ToString(),
            SupplierID = purchaseOrder.SupplierID,
            SupplierName = purchaseOrder.SupplierName ?? string.Empty,
            SupplierContactInfo = purchaseOrder.SupplierContactInfo,
            OrderID = purchaseOrder.OrderID,
            CustomerPO = purchaseOrder.CustomerPO,
            CurrencyID = purchaseOrder.CurrencyID,
            CurrencyCode = purchaseOrder.CurrencyCode ?? string.Empty,
            CurrencySymbol = purchaseOrder.CurrencySymbol ?? string.Empty,
            SubtotalAmount = purchaseOrder.SubtotalAmount,
            WHTRate = purchaseOrder.WHTRate,
            WHTAmount = purchaseOrder.WHTAmount,
            TotalAmount = purchaseOrder.TotalAmount,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            CreatedBy = purchaseOrder.CreatedBy,
            CreatedAt = purchaseOrder.CreatedAt,
            LastModifiedBy = purchaseOrder.LastModifiedBy,
            LastModifiedAt = purchaseOrder.LastModifiedAt,
            ApprovedBy = purchaseOrder.ApprovedBy,
            ApprovedAt = purchaseOrder.ApprovedAt,
            Notes = purchaseOrder.Notes,
            RowVersion = purchaseOrder.RowVersion.ToString()
        };
    }

    private static PurchaseOrderResponse MapToResponse(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderResponse
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            OrderType = purchaseOrder.OrderType.ToString(),
            Status = purchaseOrder.Status.ToString(),
            SupplierID = purchaseOrder.SupplierID,
            SupplierName = purchaseOrder.SupplierName ?? string.Empty,
            OrderID = purchaseOrder.OrderID,
            CurrencyCode = purchaseOrder.CurrencyCode ?? string.Empty,
            TotalAmount = purchaseOrder.TotalAmount,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            CreatedAt = purchaseOrder.CreatedAt
        };
    }
}
