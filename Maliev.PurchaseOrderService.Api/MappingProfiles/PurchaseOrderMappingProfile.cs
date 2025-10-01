using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Entities;

namespace Maliev.PurchaseOrderService.Api.MappingProfiles;

/// <summary>
/// AutoMapper profile for Purchase Order Service entity-to-DTO mappings
/// Provides comprehensive bidirectional mapping configuration for all DTOs
/// </summary>
public class PurchaseOrderMappingProfile : Profile
{
    /// <summary>
    /// Initializes the AutoMapper configuration for Purchase Order Service
    /// </summary>
    public PurchaseOrderMappingProfile()
    {
        ConfigurePurchaseOrderMappings();
        ConfigureOrderItemMappings();
        ConfigureAddressMappings();
        ConfigurePurchaseOrderFileMappings();
        ConfigureResponseMappings();
        ConfigureAuditLogMappings();
        ConfigureDomainEventMappings();
        ConfigureDocumentMappings();
        ConfigureSummaryMappings();
    }

    /// <summary>
    /// Configure mappings for PurchaseOrder entity and related DTOs
    /// </summary>
    private void ConfigurePurchaseOrderMappings()
    {
        // Entity to DTO mappings
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : string.Empty))
            .ForMember(dest => dest.SubtotalAmount, opt => opt.MapFrom(src => src.SubtotalAmount))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dest => dest.PurchaseOrderFiles, opt => opt.MapFrom(src => src.PurchaseOrderFiles))
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.BillingAddress, opt => opt.MapFrom(src => src.BillingAddress))
            .ForMember(dest => dest.CancelledBy, opt => opt.MapFrom(src => src.CancelledBy))
            .ForMember(dest => dest.CancelledAt, opt => opt.MapFrom(src => src.CancelledAt));

        // DTO to Entity mappings
        CreateMap<PurchaseOrderDto, PurchaseOrder>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => Convert.FromBase64String(src.RowVersion)))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dest => dest.PurchaseOrderFiles, opt => opt.MapFrom(src => src.PurchaseOrderFiles))
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.BillingAddress, opt => opt.MapFrom(src => src.BillingAddress))
            .ForMember(dest => dest.CancelledBy, opt => opt.MapFrom(src => src.CancelledBy))
            .ForMember(dest => dest.CancelledAt, opt => opt.MapFrom(src => src.CancelledAt));

        // Create request to Entity mapping
        CreateMap<CreatePurchaseOrderRequest, PurchaseOrder>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OrderNumber, opt => opt.Ignore()) // Auto-generated
            .ForMember(dest => dest.SupplierName, opt => opt.Ignore()) // Populated from external service
            .ForMember(dest => dest.SupplierContactInfo, opt => opt.Ignore()) // Populated from external service
            .ForMember(dest => dest.CurrencyCode, opt => opt.Ignore()) // Populated from external service
            .ForMember(dest => dest.CurrencySymbol, opt => opt.Ignore()) // Populated from external service
            .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Data.Enums.OrderStatus.Pending))
            .ForMember(dest => dest.SubtotalAmount, opt => opt.Ignore()) // Calculated from items
            .ForMember(dest => dest.WHTAmount, opt => opt.Ignore()) // Calculated
            .ForMember(dest => dest.TotalAmount, opt => opt.Ignore()) // Calculated
            .ForMember(dest => dest.Currency, opt => opt.Ignore()) // Populated from external service
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.OrderItems, opt => opt.Ignore()) // Populated from external service
            .ForMember(dest => dest.ShippingAddressId, opt => opt.Ignore()) // Set after address creation
            .ForMember(dest => dest.BillingAddressId, opt => opt.Ignore()) // Set after address creation
            .ForMember(dest => dest.ShippingAddress, opt => opt.Ignore())
            .ForMember(dest => dest.BillingAddress, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrderFiles, opt => opt.Ignore())
            .ForMember(dest => dest.WHTRate, opt => opt.MapFrom(src => src.WhtRate));

        // Update request to Entity mapping
        CreateMap<UpdatePurchaseOrderRequest, PurchaseOrder>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
            .ForMember(dest => dest.SupplierID, opt => opt.Ignore()) // Cannot be changed
            .ForMember(dest => dest.OrderID, opt => opt.Ignore()) // Cannot be changed
            .ForMember(dest => dest.SupplierName, opt => opt.Ignore())
            .ForMember(dest => dest.SupplierContactInfo, opt => opt.Ignore())
            .ForMember(dest => dest.CurrencyCode, opt => opt.Ignore()) // Updated from external service if CurrencyID changes
            .ForMember(dest => dest.CurrencySymbol, opt => opt.Ignore())
            .ForMember(dest => dest.OrderDate, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.OrderType, opt => opt.Ignore())
            .ForMember(dest => dest.SubtotalAmount, opt => opt.Ignore()) // Recalculated if currency changes
            .ForMember(dest => dest.WHTAmount, opt => opt.Ignore()) // Recalculated
            .ForMember(dest => dest.TotalAmount, opt => opt.Ignore()) // Recalculated
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => Convert.FromBase64String(src.RowVersion)))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
            .ForMember(dest => dest.ShippingAddressId, opt => opt.Ignore())
            .ForMember(dest => dest.BillingAddressId, opt => opt.Ignore())
            .ForMember(dest => dest.ShippingAddress, opt => opt.Ignore())
            .ForMember(dest => dest.BillingAddress, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrderFiles, opt => opt.Ignore())
            .ForMember(dest => dest.WHTRate, opt => opt.MapFrom(src => src.WhtRate));
    }

    /// <summary>
    /// Configure mappings for OrderItem entity and related DTOs
    /// </summary>
    private void ConfigureOrderItemMappings()
    {
        // Entity to DTO mappings
        CreateMap<OrderItem, OrderItemDto>();

        // DTO to Entity mappings
        CreateMap<OrderItemDto, OrderItem>()
            .ForMember(dest => dest.PurchaseOrder, opt => opt.Ignore());

        // Create request to Entity mapping
        CreateMap<CreateOrderItemRequest, OrderItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrderId, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.ExternalOrderItemId, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Quantity * src.UnitPrice))
            .ForMember(dest => dest.Currency, opt => opt.Ignore()) // Set from parent PO
            .ForMember(dest => dest.CachedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ExternallyModified, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.PurchaseOrder, opt => opt.Ignore());

        // Update request to Entity mapping
        CreateMap<UpdateOrderItemRequest, OrderItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrderId, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalOrderItemId, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom((src, dest) =>
                (src.Quantity ?? dest.Quantity) * (src.UnitPrice ?? dest.UnitPrice)))
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.CachedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ExternallyModified, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrder, opt => opt.Ignore());
    }

    /// <summary>
    /// Configure mappings for Address entity and related DTOs
    /// </summary>
    private void ConfigureAddressMappings()
    {
        // Entity to DTO mappings
        CreateMap<Address, AddressDto>();

        // DTO to Entity mappings
        CreateMap<AddressDto, Address>()
            .ForMember(dest => dest.ShippingPurchaseOrders, opt => opt.Ignore())
            .ForMember(dest => dest.BillingPurchaseOrders, opt => opt.Ignore());

        // Create request to Entity mapping
        CreateMap<CreateAddressRequest, Address>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ShippingPurchaseOrders, opt => opt.Ignore())
            .ForMember(dest => dest.BillingPurchaseOrders, opt => opt.Ignore());

        // Update request to Entity mapping
        CreateMap<UpdateAddressRequest, Address>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AddressType, opt => opt.Ignore()) // Cannot be changed
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ShippingPurchaseOrders, opt => opt.Ignore())
            .ForMember(dest => dest.BillingPurchaseOrders, opt => opt.Ignore());
    }

    /// <summary>
    /// Configure mappings for PurchaseOrderFile entity and related DTOs
    /// </summary>
    private void ConfigurePurchaseOrderFileMappings()
    {
        // Entity to DTO mappings
        CreateMap<PurchaseOrderFile, PurchaseOrderFileDto>();

        // DTO to Entity mappings
        CreateMap<PurchaseOrderFileDto, PurchaseOrderFile>()
            .ForMember(dest => dest.PurchaseOrder, opt => opt.Ignore());

        // Upload request to Entity mapping
        CreateMap<UploadFileRequest, PurchaseOrderFile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrderId, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.File.FileName))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.File.Length))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.File.ContentType))
            .ForMember(dest => dest.UploadedBy, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.PurchaseOrder, opt => opt.Ignore());

        // Entity to upload response mapping
        CreateMap<PurchaseOrderFile, FileUploadResponse>()
            .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.UploadedAt))
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedBy));

        // Entity to download response mapping
        CreateMap<PurchaseOrderFile, FileDownloadResponse>()
            .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize));
    }

    /// <summary>
    /// Configure mappings for response DTOs
    /// </summary>
    private void ConfigureResponseMappings()
    {
        // PurchaseOrder to Response mappings
        CreateMap<PurchaseOrder, PurchaseOrderResponse>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : string.Empty))
            .ForMember(dest => dest.SubtotalAmount, opt => opt.MapFrom(src => src.SubtotalAmount))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.WhtRate, opt => opt.MapFrom(src => src.WHTRate))
            .ForMember(dest => dest.WhtAmount, opt => opt.MapFrom(src => src.WHTAmount));

        // PurchaseOrderDto to Response mapping
        CreateMap<PurchaseOrderDto, PurchaseOrderResponse>()
            .ForMember(dest => dest.WhtRate, opt => opt.MapFrom(src => src.WHTRate))
            .ForMember(dest => dest.WhtAmount, opt => opt.MapFrom(src => src.WHTAmount));

        CreateMap<PurchaseOrder, PurchaseOrderDetailResponse>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : string.Empty))
            .ForMember(dest => dest.SubtotalAmount, opt => opt.MapFrom(src => src.SubtotalAmount))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dest => dest.PurchaseOrderFiles, opt => opt.MapFrom(src => src.PurchaseOrderFiles))
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.BillingAddress, opt => opt.MapFrom(src => src.BillingAddress));

        // OrderItem to Response mapping
        CreateMap<OrderItem, OrderItemResponse>();

        // Address to Response mapping
        CreateMap<Address, AddressResponse>();

        // PurchaseOrderFile to Response mapping
        CreateMap<PurchaseOrderFile, PurchaseOrderFileDto>();
    }

    /// <summary>
    /// Configure mappings for AuditLog entity and related DTOs
    /// </summary>
    private void ConfigureAuditLogMappings()
    {
        // Entity to DTO mapping
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.Id)) // Convert long to int for DTO
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => $"{src.Action} on {src.EntityType} {src.EntityId}" + (src.ExternalServiceName != null ? $" via {src.ExternalServiceName}" : "")))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.OriginalData, opt => opt.MapFrom(src => src.OldValues))
            .ForMember(dest => dest.NewData, opt => opt.MapFrom(src => src.NewValues))
            .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IPAddress))
            .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent));
    }

    /// <summary>
    /// Configure mappings for DomainEvent entity and related DTOs
    /// </summary>
    private void ConfigureDomainEventMappings()
    {
        // Entity to DTO mapping
        CreateMap<DomainEvent, DomainEventDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.CorrelationId, opt => opt.MapFrom(src => src.CorrelationId));
    }

    /// <summary>
    /// Configure mappings for document-related DTOs
    /// </summary>
    private void ConfigureDocumentMappings()
    {
        // PurchaseOrderFile to DocumentUploadResult mapping
        CreateMap<PurchaseOrderFile, DocumentUploadResult>()
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.File, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.ObjectName))
            .ForMember(dest => dest.FileHash, opt => opt.Ignore()) // Set by service if needed
            .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.UploadedAt))
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedBy));

        // PurchaseOrderFile to DocumentDownloadResult mapping
        CreateMap<PurchaseOrderFile, DocumentDownloadResult>()
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.FileStream, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize))
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.FileMetadata, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.LastModified, opt => opt.MapFrom(src => src.UploadedAt))
            .ForMember(dest => dest.ETag, opt => opt.Ignore()); // Set by service if needed
    }

    /// <summary>
    /// Configure mappings for summary and statistical DTOs
    /// </summary>
    private void ConfigureSummaryMappings()
    {
        // Custom mapping for OrderItemsSummaryDto would be handled by service layer
        // as it requires aggregation logic across multiple OrderItem entities
        // This is a calculated DTO that doesn't map directly from a single entity

        // Note: Statistical DTOs like PurchaseOrderStatsDto, MonthlyStatsDto, CurrencyStatsDto,
        // and SupplierStatsDto are generated from complex aggregation queries and business logic
        // in the service layer. They don't have direct entity mappings.
    }

    // Note: Custom value resolvers were replaced with inline expressions for better performance
    // and simpler maintenance. The business logic for calculated fields like TotalAmount,
    // SubtotalAmount, and RowVersion handling is now implemented directly in the mapping
    // expressions above. This approach is more maintainable and provides better compile-time
    // type safety while still handling all the necessary business logic and error cases.
}