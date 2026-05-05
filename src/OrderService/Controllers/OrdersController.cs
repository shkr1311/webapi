using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(new { success = true, data = orders });
    }

    /// <summary>
    /// Get an order by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(new { success = false, message = $"Order with ID {id} not found" });

        return Ok(new { success = true, data = order });
    }

    /// <summary>
    /// Create a new order (validates product + assigns employee)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ModelState });

        var order = await _orderService.CreateOrderAsync(dto);
        _logger.LogInformation("Order created: {OrderId}", order.Id);

        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            new { success = true, data = order, message = "Order created successfully" });
    }

    /// <summary>
    /// Mark order as delivered
    /// </summary>
    [HttpPut("{id:int}/deliver")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> MarkDelivered(int id)
    {
        var order = await _orderService.MarkAsDeliveredAsync(id);
        if (order == null)
            return NotFound(new { success = false, message = $"Order with ID {id} not found" });

        return Ok(new { success = true, data = order, message = "Order marked as delivered" });
    }

    /// <summary>
    /// Mark COD payment as paid
    /// </summary>
    [HttpPut("{id:int}/pay")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> MarkPaid(int id)
    {
        var order = await _orderService.MarkAsPaidAsync(id);
        if (order == null)
            return NotFound(new { success = false, message = $"Order with ID {id} not found" });

        return Ok(new { success = true, data = order, message = "Payment marked as COD Paid" });
    }
}
