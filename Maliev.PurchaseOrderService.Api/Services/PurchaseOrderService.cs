namespace Maliev.PurchaseOrderService.Api.Services
{
    using AutoMapper;
    using Maliev.PurchaseOrderService.Api.DTOs;
    using Maliev.PurchaseOrderService.Data;
    using Maliev.PurchaseOrderService.Data.Entities;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly PurchaseOrderContext _context;
        private readonly IMapper _mapper;

        public PurchaseOrderService(PurchaseOrderContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Purchase Orders
        public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
        {
            var purchaseOrders = await _context.PurchaseOrder
                .Include(po => po.OrderItem)
                .Include(po => po.PurchaseOrderFile)
                .Include(po => po.BillingAddress)
                .Include(po => po.ShippingAddress)
                .ToListAsync();
            return _mapper.Map<IEnumerable<PurchaseOrderDto>>(purchaseOrders);
        }

        public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id)
        {
            var purchaseOrder = await _context.PurchaseOrder
                .Include(po => po.OrderItem)
                .Include(po => po.PurchaseOrderFile)
                .Include(po => po.BillingAddress)
                .Include(po => po.ShippingAddress)
                .FirstOrDefaultAsync(po => po.Id == id);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }

        public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto purchaseOrderDto)
        {
            var purchaseOrder = _mapper.Map<PurchaseOrder>(purchaseOrderDto);
            _context.PurchaseOrder.Add(purchaseOrder);
            await _context.SaveChangesAsync();
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }

        public async Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderDto purchaseOrderDto)
        {
            var existingPurchaseOrder = await _context.PurchaseOrder.FindAsync(id);
            if (existingPurchaseOrder == null)
            {
                return null;
            }

            _mapper.Map(purchaseOrderDto, existingPurchaseOrder);
            await _context.SaveChangesAsync();
            return _mapper.Map<PurchaseOrderDto>(existingPurchaseOrder);
        }

        public async Task<bool> DeletePurchaseOrderAsync(int id)
        {
            var purchaseOrder = await _context.PurchaseOrder.FindAsync(id);
            if (purchaseOrder == null)
            {
                return false;
            }

            _context.PurchaseOrder.Remove(purchaseOrder);
            await _context.SaveChangesAsync();
            return true;
        }

        // Order Items
        public async Task<IEnumerable<OrderItemDto>> GetOrderItemsByPurchaseOrderIdAsync(int purchaseOrderId)
        {
            var orderItems = await _context.OrderItem
                .Where(oi => oi.PurchaseOrderId == purchaseOrderId)
                .ToListAsync();
            return _mapper.Map<IEnumerable<OrderItemDto>>(orderItems);
        }

        public async Task<OrderItemDto?> GetOrderItemByIdAsync(int id)
        {
            var orderItem = await _context.OrderItem.FindAsync(id);
            return _mapper.Map<OrderItemDto>(orderItem);
        }

        public async Task<OrderItemDto> CreateOrderItemAsync(CreateOrderItemDto orderItemDto)
        {
            var orderItem = _mapper.Map<OrderItem>(orderItemDto);
            _context.OrderItem.Add(orderItem);
            await _context.SaveChangesAsync();
            return _mapper.Map<OrderItemDto>(orderItem);
        }

        public async Task<OrderItemDto?> UpdateOrderItemAsync(int id, UpdateOrderItemDto orderItemDto)
        {
            var existingOrderItem = await _context.OrderItem.FindAsync(id);
            if (existingOrderItem == null)
            {
                return null;
            }

            _mapper.Map(orderItemDto, existingOrderItem);
            await _context.SaveChangesAsync();
            return _mapper.Map<OrderItemDto>(existingOrderItem);
        }

        public async Task<bool> DeleteOrderItemAsync(int id)
        {
            var orderItem = await _context.OrderItem.FindAsync(id);
            if (orderItem == null)
            {
                return false;
            }

            _context.OrderItem.Remove(orderItem);
            await _context.SaveChangesAsync();
            return true;
        }

        // Addresses
        public async Task<IEnumerable<AddressDto>> GetAllAddressesAsync()
        {
            var addresses = await _context.Address.ToListAsync();
            return _mapper.Map<IEnumerable<AddressDto>>(addresses);
        }

        public async Task<AddressDto?> GetAddressByIdAsync(int id)
        {
            var address = await _context.Address.FindAsync(id);
            return _mapper.Map<AddressDto>(address);
        }

        public async Task<AddressDto> CreateAddressAsync(CreateAddressDto addressDto)
        {
            var address = _mapper.Map<Address>(addressDto);
            _context.Address.Add(address);
            await _context.SaveChangesAsync();
            return _mapper.Map<AddressDto>(address);
        }

        public async Task<AddressDto?> UpdateAddressAsync(int id, UpdateAddressDto addressDto)
        {
            var existingAddress = await _context.Address.FindAsync(id);
            if (existingAddress == null)
            {
                return null;
            }

            _mapper.Map(addressDto, existingAddress);
            await _context.SaveChangesAsync();
            return _mapper.Map<AddressDto>(existingAddress);
        }

        public async Task<bool> DeleteAddressAsync(int id)
        {
            var address = await _context.Address.FindAsync(id);
            if (address == null)
            {
                return false;
            }

            _context.Address.Remove(address);
            await _context.SaveChangesAsync();
            return true;
        }

        // Purchase Order Files
        public async Task<IEnumerable<PurchaseOrderFileDto>> GetPurchaseOrderFilesByPurchaseOrderIdAsync(int purchaseOrderId)
        {
            var purchaseOrderFiles = await _context.PurchaseOrderFile
                .Where(pof => pof.PurchaseOrderId == purchaseOrderId)
                .ToListAsync();
            return _mapper.Map<IEnumerable<PurchaseOrderFileDto>>(purchaseOrderFiles);
        }

        public async Task<PurchaseOrderFileDto?> GetPurchaseOrderFileByIdAsync(int id)
        {
            var purchaseOrderFile = await _context.PurchaseOrderFile.FindAsync(id);
            return _mapper.Map<PurchaseOrderFileDto>(purchaseOrderFile);
        }

        public async Task<PurchaseOrderFileDto> CreatePurchaseOrderFileAsync(CreatePurchaseOrderFileDto purchaseOrderFileDto)
        {
            var purchaseOrderFile = _mapper.Map<PurchaseOrderFile>(purchaseOrderFileDto);
            _context.PurchaseOrderFile.Add(purchaseOrderFile);
            await _context.SaveChangesAsync();
            return _mapper.Map<PurchaseOrderFileDto>(purchaseOrderFile);
        }

        public async Task<PurchaseOrderFileDto?> UpdatePurchaseOrderFileAsync(int id, UpdatePurchaseOrderFileDto purchaseOrderFileDto)
        {
            var existingPurchaseOrderFile = await _context.PurchaseOrderFile.FindAsync(id);
            if (existingPurchaseOrderFile == null)
            {
                return null;
            }

            _mapper.Map(purchaseOrderFileDto, existingPurchaseOrderFile);
            await _context.SaveChangesAsync();
            return _mapper.Map<PurchaseOrderFileDto>(existingPurchaseOrderFile);
        }

        public async Task<bool> DeletePurchaseOrderFileAsync(int id)
        {
            var purchaseOrderFile = await _context.PurchaseOrderFile.FindAsync(id);
            if (purchaseOrderFile == null)
            {
                return false;
            }

            _context.PurchaseOrderFile.Remove(purchaseOrderFile);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
