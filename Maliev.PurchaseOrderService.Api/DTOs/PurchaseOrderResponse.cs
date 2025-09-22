using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

public class PurchaseOrderResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public int SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int OrderID { get; set; }
    public int CurrencyID { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public OrderStatus Status { get; set; }
    public OrderType OrderType { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal? WhtRate { get; set; }
    public decimal? WhtAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}