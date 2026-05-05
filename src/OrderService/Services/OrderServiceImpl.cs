using AutoMapper;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories;
using System.Text.Json;

namespace OrderService.Services;

public class OrderServiceImpl : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderServiceImpl> _logger;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderServiceImpl(
        IOrderRepository repository,
        IHttpClientFactory httpClientFactory,
        IMapper mapper,
        ILogger<OrderServiceImpl> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", id);
            return null;
        }
        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        _logger.LogInformation("Creating order for ProductId: {ProductId}, Quantity: {Quantity}",
            dto.ProductId, dto.Quantity);

        // Step 1: Validate product exists via Product Service
        var product = await ValidateProductAsync(dto.ProductId);
        if (product == null)
            throw new InvalidOperationException($"Product with ID {dto.ProductId} not found or Product Service is unavailable");

        if (product.Stock < dto.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {product.Stock}, Requested: {dto.Quantity}");

        // Step 2: Assign available employee via Employee Service
        var employee = await GetAvailableEmployeeAsync();
        if (employee == null)
            throw new InvalidOperationException("No available delivery employees at the moment");

        // Step 3: Mark employee as unavailable
        await UpdateEmployeeAvailabilityAsync(employee.Id, false);

        // Step 4: Create the order
        var order = new Order
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            EmployeeId = employee.Id,
            Status = OrderStatus.Created,
            PaymentStatus = PaymentStatusType.Pending,
            ProductName = product.Name,
            ProductPrice = product.Price,
            EmployeeName = employee.Name,
            TotalAmount = product.Price * dto.Quantity
        };

        var created = await _repository.CreateAsync(order);
        _logger.LogInformation(
            "Order {OrderId} created successfully. Product: {ProductName}, Employee: {EmployeeName}, Total: {Total}",
            created.Id, product.Name, employee.Name, order.TotalAmount);

        return _mapper.Map<OrderDto>(created);
    }

    public async Task<OrderDto?> MarkAsDeliveredAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Cannot deliver - Order with ID {OrderId} not found", id);
            return null;
        }

        if (order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException($"Order {id} is already delivered");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException($"Order {id} is cancelled and cannot be delivered");

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        // Release the employee
        if (order.EmployeeId.HasValue)
        {
            await UpdateEmployeeAvailabilityAsync(order.EmployeeId.Value, true);
        }

        var updated = await _repository.UpdateAsync(order);
        _logger.LogInformation("Order {OrderId} marked as delivered", id);
        return updated != null ? _mapper.Map<OrderDto>(updated) : null;
    }

    public async Task<OrderDto?> MarkAsPaidAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Cannot mark paid - Order with ID {OrderId} not found", id);
            return null;
        }

        if (order.PaymentStatus == PaymentStatusType.Paid)
            throw new InvalidOperationException($"Order {id} is already paid");

        if (order.Status != OrderStatus.Delivered)
            throw new InvalidOperationException($"Order {id} must be delivered before payment can be collected");

        order.PaymentStatus = PaymentStatusType.Paid;
        order.PaidAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(order);
        _logger.LogInformation("Order {OrderId} payment marked as COD Paid", id);
        return updated != null ? _mapper.Map<OrderDto>(updated) : null;
    }

    // ──────────────────────────────────────────────
    // Inter-Service Communication
    // ──────────────────────────────────────────────

    private async Task<ProductResponseDto?> ValidateProductAsync(int productId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ProductService");
            var response = await client.GetAsync($"/api/products/{productId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Product Service returned {StatusCode} for ProductId: {ProductId}",
                    response.StatusCode, productId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductResponseDto>>(content, JsonOptions);
            return apiResponse?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Product Service for ProductId: {ProductId}", productId);
            throw new InvalidOperationException("Product Service is unavailable. Please try again later.", ex);
        }
    }

    private async Task<EmployeeResponseDto?> GetAvailableEmployeeAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("EmployeeService");
            var response = await client.GetAsync("/api/employees/available");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Employee Service returned {StatusCode} - No available employees",
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<EmployeeResponseDto>>(content, JsonOptions);
            return apiResponse?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Employee Service");
            throw new InvalidOperationException("Employee Service is unavailable. Please try again later.", ex);
        }
    }

    private async Task UpdateEmployeeAvailabilityAsync(int employeeId, bool isAvailable)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("EmployeeService");
            var response = await client.PutAsJsonAsync(
                $"/api/employees/{employeeId}/availability",
                new { IsAvailable = isAvailable });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to update employee {EmployeeId} availability to {IsAvailable}. Status: {StatusCode}",
                    employeeId, isAvailable, response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Employee {EmployeeId} availability updated to {IsAvailable}",
                    employeeId, isAvailable);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Employee Service to update availability");
            // Non-critical - don't fail the order operation
        }
    }
}
