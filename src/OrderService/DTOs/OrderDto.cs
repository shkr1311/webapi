namespace OrderService.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal? ProductPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class CreateOrderDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

// DTOs for inter-service communication responses
public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class EmployeeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

// Wrapper for API responses from other services
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
