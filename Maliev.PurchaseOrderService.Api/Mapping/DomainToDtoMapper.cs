using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Common.Enumerations;
using Maliev.PurchaseOrderService.Data.Entities;

namespace Maliev.PurchaseOrderService.Api.Mapping;

/// <summary>
/// Pure .NET mapper for purchase order-related domain models and DTOs
/// </summary>
public static class DomainToDtoMapper
{
    /// <summary>Maps a PurchaseOrder entity to PurchaseOrderDetailResponse</summary>
    public static PurchaseOrderDetailResponse ToPurchaseOrderDetailResponse(this PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderDetailResponse
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            OrderType = purchaseOrder.OrderType,
            Status = purchaseOrder.Status,
            SupplierID = purchaseOrder.SupplierID,
            SupplierName = purchaseOrder.SupplierName,
            SupplierContactInfo = purchaseOrder.SupplierContactInfo,
            OrderID = purchaseOrder.OrderID,
            CustomerPO = purchaseOrder.CustomerPO,
            CurrencyID = purchaseOrder.CurrencyID,
            CurrencyCode = purchaseOrder.CurrencyCode,
            CurrencySymbol = purchaseOrder.CurrencySymbol,
            SubtotalAmount = purchaseOrder.SubtotalAmount,
            WHTRate = purchaseOrder.WHTRate,
            WHTAmount = purchaseOrder.WHTAmount,
            TotalAmount = purchaseOrder.TotalAmount,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            ShippingAddress = purchaseOrder.ShippingAddress?.ToAddressResponse(),
            BillingAddress = purchaseOrder.BillingAddress?.ToAddressResponse(),
            Notes = purchaseOrder.Notes,
            Items = purchaseOrder.Items?.Select(i => i.ToOrderItemResponse()).ToList() ?? new List<OrderItemResponse>(),
            Files = purchaseOrder.Files?.Select(f => f.ToPurchaseOrderFileResponse()).ToList() ?? new List<PurchaseOrderFileResponse>(),
            CreatedBy = purchaseOrder.CreatedBy,
            CreatedAt = purchaseOrder.CreatedAt,
            LastModifiedBy = purchaseOrder.LastModifiedBy,
            LastModifiedAt = purchaseOrder.LastModifiedAt,
            RowVersion = purchaseOrder.RowVersion.ToString()
        };
    }

    /// <summary>Maps a PurchaseOrder entity to PurchaseOrderResponse</summary>
    public static PurchaseOrderResponse ToPurchaseOrderResponse(this PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderResponse
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            OrderType = purchaseOrder.OrderType,
            Status = purchaseOrder.Status,
            SupplierID = purchaseOrder.SupplierID,
            SupplierName = purchaseOrder.SupplierName,
            OrderID = purchaseOrder.OrderID,
            CurrencyCode = purchaseOrder.CurrencyCode,
            TotalAmount = purchaseOrder.TotalAmount,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            CreatedAt = purchaseOrder.CreatedAt
        };
    }

    /// <summary>Maps CreatePurchaseOrderRequest to PurchaseOrder entity</summary>
    public static PurchaseOrder ToPurchaseOrder(this CreatePurchaseOrderRequest request)
    {
        return new PurchaseOrder
        {
            OrderType = request.OrderType,
            SupplierID = request.SupplierID,
            OrderID = request.OrderID,
            CustomerPO = request.CustomerPO,
            CurrencyID = request.CurrencyID,
            WHTRate = request.WHTRate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes
        };
    }

    /// <summary>Maps an OrderItem entity to OrderItemResponse</summary>
    public static OrderItemResponse ToOrderItemResponse(this OrderItem item)
    {
        return new OrderItemResponse
        {
            Id = item.Id,
            ExternalOrderItemId = item.ExternalOrderItemId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitOfMeasure = item.UnitOfMeasure,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            Currency = item.Currency,
            Notes = item.Notes,
            CachedAt = item.CachedAt,
            ExternallyModified = item.ExternallyModified
        };
    }

    /// <summary>Maps an Address entity to AddressResponse</summary>
    public static AddressResponse ToAddressResponse(this Address address)
    {
        return new AddressResponse
        {
            Id = address.Id,
            AddressType = address.AddressType,
            CompanyName = address.CompanyName,
            ContactName = address.ContactName,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            PostalCode = address.PostalCode,
            Country = address.Country,
            PhoneNumber = address.PhoneNumber,
            EmailAddress = address.EmailAddress
        };
    }

    /// <summary>Maps CreateAddressRequest to Address entity</summary>
    public static Address ToAddress(this CreateAddressRequest request)
    {
        return new Address
        {
            AddressType = request.AddressType,
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            Country = request.Country,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress
        };
    }

    /// <summary>Maps UpdateAddressRequest to Address entity</summary>
    public static Address ToAddress(this UpdateAddressRequest request)
    {
        return new Address
        {
            AddressType = request.AddressType ?? AddressType.Shipping,
            CompanyName = request.CompanyName,
            ContactName = request.ContactName ?? string.Empty,
            AddressLine1 = request.AddressLine1 ?? string.Empty,
            AddressLine2 = request.AddressLine2,
            City = request.City ?? string.Empty,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode ?? string.Empty,
            Country = request.Country ?? string.Empty,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress
        };
    }

    /// <summary>Maps a PurchaseOrderFile entity to PurchaseOrderFileResponse</summary>
    public static PurchaseOrderFileResponse ToPurchaseOrderFileResponse(this PurchaseOrderFile file)
    {
        return new PurchaseOrderFileResponse
        {
            Id = file.Id,
            PurchaseOrderId = file.PurchaseOrderId,
            FileName = file.FileName,
            ObjectName = file.ObjectName,
            FileSize = file.FileSize,
            ContentType = file.ContentType,
            DocumentType = file.DocumentType,
            UploadedAt = file.UploadedAt,
            UploadedBy = file.UploadedBy,
            Description = file.Description
        };
    }
}
