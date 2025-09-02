namespace Maliev.PurchaseOrderService.Api.MappingProfiles
{
    using AutoMapper;
    using Maliev.PurchaseOrderService.Api.DTOs;
    using Maliev.PurchaseOrderService.Data.Entities;

    public class PurchaseOrderMappingProfile : Profile
    {
        public PurchaseOrderMappingProfile()
        {
            CreateMap<Address, AddressDto>().ReverseMap();
            CreateMap<CreateAddressDto, Address>();
            CreateMap<UpdateAddressDto, Address>();

            CreateMap<OrderItem, OrderItemDto>().ReverseMap();
            CreateMap<CreateOrderItemDto, OrderItem>();
            CreateMap<UpdateOrderItemDto, OrderItem>();

            CreateMap<PurchaseOrder, PurchaseOrderDto>().ReverseMap();
            CreateMap<CreatePurchaseOrderDto, PurchaseOrder>();
            CreateMap<UpdatePurchaseOrderDto, PurchaseOrder>();

            CreateMap<PurchaseOrderFile, PurchaseOrderFileDto>().ReverseMap();
            CreateMap<CreatePurchaseOrderFileDto, PurchaseOrderFile>();
            CreateMap<UpdatePurchaseOrderFileDto, PurchaseOrderFile>();
        }
    }
}
