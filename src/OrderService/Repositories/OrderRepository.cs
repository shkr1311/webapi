using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all orders from database");
        return await _context.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching order with ID: {OrderId}", id);
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _logger.LogInformation("Creating new order for Product: {ProductId}", order.ProductId);
        order.CreatedAt = DateTime.UtcNow;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateAsync(Order order)
    {
        var existing = await _context.Orders.FindAsync(order.Id);
        if (existing == null) return null;

        _context.Entry(existing).CurrentValues.SetValues(order);
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated order with ID: {OrderId}", order.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted order with ID: {OrderId}", id);
        return true;
    }
}
