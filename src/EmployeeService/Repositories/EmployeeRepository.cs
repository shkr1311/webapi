using Microsoft.EntityFrameworkCore;
using EmployeeService.Data;
using EmployeeService.Models;

namespace EmployeeService.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly EmployeeDbContext _context;
    private readonly ILogger<EmployeeRepository> _logger;

    public EmployeeRepository(EmployeeDbContext context, ILogger<EmployeeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all employees from database");
        return await _context.Employees.AsNoTracking().OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching employee with ID: {EmployeeId}", id);
        return await _context.Employees.FindAsync(id);
    }

    public async Task<Employee?> GetFirstAvailableAsync()
    {
        _logger.LogInformation("Fetching first available employee");
        return await _context.Employees
            .Where(e => e.IsAvailable)
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        _logger.LogInformation("Creating new employee: {EmployeeName}", employee.Name);
        employee.CreatedAt = DateTime.UtcNow;
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee?> UpdateAsync(Employee employee)
    {
        var existing = await _context.Employees.FindAsync(employee.Id);
        if (existing == null) return null;

        existing.Name = employee.Name;
        existing.Phone = employee.Phone;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated employee with ID: {EmployeeId}", employee.Id);
        return existing;
    }

    public async Task<bool> UpdateAvailabilityAsync(int id, bool isAvailable)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return false;

        employee.IsAvailable = isAvailable;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} availability set to {IsAvailable}", id, isAvailable);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return false;

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted employee with ID: {EmployeeId}", id);
        return true;
    }
}
