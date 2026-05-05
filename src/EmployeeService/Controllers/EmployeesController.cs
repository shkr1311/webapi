using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeService.DTOs;
using EmployeeService.Services;

namespace EmployeeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await _employeeService.GetAllEmployeesAsync();
        return Ok(new { success = true, data = employees });
    }

    /// <summary>
    /// Get an employee by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null)
            return NotFound(new { success = false, message = $"Employee with ID {id} not found" });

        return Ok(new { success = true, data = employee });
    }

    /// <summary>
    /// Get first available employee (used by Order Service for assignment)
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetAvailable()
    {
        var employee = await _employeeService.GetFirstAvailableEmployeeAsync();
        if (employee == null)
            return NotFound(new { success = false, message = "No available employees found" });

        return Ok(new { success = true, data = employee });
    }

    /// <summary>
    /// Create a new employee
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ModelState });

        var employee = await _employeeService.CreateEmployeeAsync(dto);
        _logger.LogInformation("Employee created: {EmployeeId}", employee.Id);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id },
            new { success = true, data = employee, message = "Employee created successfully" });
    }

    /// <summary>
    /// Update employee availability
    /// </summary>
    [HttpPut("{id:int}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailability(int id, [FromBody] AvailabilityDto dto)
    {
        var result = await _employeeService.UpdateAvailabilityAsync(id, dto.IsAvailable);
        if (!result)
            return NotFound(new { success = false, message = $"Employee with ID {id} not found" });

        return Ok(new { success = true, message = $"Employee availability updated to {dto.IsAvailable}" });
    }

    /// <summary>
    /// Delete an employee
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _employeeService.DeleteEmployeeAsync(id);
        if (!result)
            return NotFound(new { success = false, message = $"Employee with ID {id} not found" });

        return Ok(new { success = true, message = "Employee deleted successfully" });
    }
}
