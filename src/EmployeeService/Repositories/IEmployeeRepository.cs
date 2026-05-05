using EmployeeService.Models;

namespace EmployeeService.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee?> GetFirstAvailableAsync();
    Task<Employee> CreateAsync(Employee employee);
    Task<Employee?> UpdateAsync(Employee employee);
    Task<bool> UpdateAvailabilityAsync(int id, bool isAvailable);
    Task<bool> DeleteAsync(int id);
}
