using OrderService.DTOs;

namespace OrderService.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderDto?> MarkAsDeliveredAsync(int id);
    Task<OrderDto?> MarkAsPaidAsync(int id);
}
