using System.ComponentModel.DataAnnotations;
using Maliev.PurchaseOrderService.Api.Attributes;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

public class CreatePurchaseOrderRequest
{
    [Required]
    public int SupplierID { get; set; }

    [Required]
    public int OrderID { get; set; }

    [Required]
    public int CurrencyID { get; set; }

    [Required]
    public OrderType OrderType { get; set; }

    [MaxLength(50)]
    public string? CustomerPO { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [WHTRateValidation]
    public decimal? WhtRate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public CreateAddressRequest? ShippingAddress { get; set; }

    public CreateAddressRequest? BillingAddress { get; set; }

    public List<CreateOrderItemRequest>? OrderItems { get; set; }
}