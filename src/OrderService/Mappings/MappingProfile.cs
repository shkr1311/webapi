using AutoMapper;
using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<CreateOrderDto, Order>();
    }
}
