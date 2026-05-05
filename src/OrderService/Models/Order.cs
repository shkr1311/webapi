using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    public int? EmployeeId { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = OrderStatus.Created;

    [MaxLength(50)]
    public string PaymentStatus { get; set; } = PaymentStatusType.Pending;

    // Denormalized fields for quick reference
    public string? ProductName { get; set; }
    public decimal? ProductPrice { get; set; }
    public string? EmployeeName { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public static class OrderStatus
{
    public const string Created = "Created";
    public const string Assigned = "Assigned";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}

public static class PaymentStatusType
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
}
