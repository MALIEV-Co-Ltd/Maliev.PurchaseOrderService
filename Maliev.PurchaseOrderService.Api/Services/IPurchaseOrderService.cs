namespace Maliev.PurchaseOrderService.Api.Services
{
    using Maliev.PurchaseOrderService.Api.DTOs;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPurchaseOrderService
    {
        // Purchase Orders
        Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
        Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id);
        Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto purchaseOrderDto);
        Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderDto purchaseOrderDto);
        Task<bool> DeletePurchaseOrderAsync(int id);

        // Order Items
        Task<IEnumerable<OrderItemDto>> GetOrderItemsByPurchaseOrderIdAsync(int purchaseOrderId);
        Task<OrderItemDto?> GetOrderItemByIdAsync(int id);
        Task<OrderItemDto> CreateOrderItemAsync(CreateOrderItemDto orderItemDto);
        Task<OrderItemDto?> UpdateOrderItemAsync(int id, UpdateOrderItemDto orderItemDto);
        Task<bool> DeleteOrderItemAsync(int id);

        // Addresses
        Task<IEnumerable<AddressDto>> GetAllAddressesAsync();
        Task<AddressDto?> GetAddressByIdAsync(int id);
        Task<AddressDto> CreateAddressAsync(CreateAddressDto addressDto);
        Task<AddressDto?> UpdateAddressAsync(int id, UpdateAddressDto addressDto);
        Task<bool> DeleteAddressAsync(int id);

        // Purchase Order Files
        Task<IEnumerable<PurchaseOrderFileDto>> GetPurchaseOrderFilesByPurchaseOrderIdAsync(int purchaseOrderId);
        Task<PurchaseOrderFileDto?> GetPurchaseOrderFileByIdAsync(int id);
        Task<PurchaseOrderFileDto> CreatePurchaseOrderFileAsync(CreatePurchaseOrderFileDto purchaseOrderFileDto);
        Task<PurchaseOrderFileDto?> UpdatePurchaseOrderFileAsync(int id, UpdatePurchaseOrderFileDto purchaseOrderFileDto);
        Task<bool> DeletePurchaseOrderFileAsync(int id);
    }
}
