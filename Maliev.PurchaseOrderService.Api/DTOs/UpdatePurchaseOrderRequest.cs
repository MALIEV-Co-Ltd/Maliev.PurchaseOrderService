using System.ComponentModel.DataAnnotations;
using Maliev.PurchaseOrderService.Api.Attributes;

namespace Maliev.PurchaseOrderService.Api.DTOs;

public class UpdatePurchaseOrderRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public int? CurrencyID { get; set; }

    [MaxLength(50)]
    public string? CustomerPO { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [WHTRateValidation]
    public decimal? WhtRate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public UpdateAddressRequest? ShippingAddress { get; set; }

    public UpdateAddressRequest? BillingAddress { get; set; }

    public List<UpdateOrderItemRequest>? OrderItems { get; set; }
}