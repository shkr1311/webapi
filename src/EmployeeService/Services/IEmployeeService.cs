using EmployeeService.DTOs;

namespace EmployeeService.Services;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<EmployeeDto?> GetFirstAvailableEmployeeAsync();
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<bool> UpdateAvailabilityAsync(int id, bool isAvailable);
    Task<bool> DeleteEmployeeAsync(int id);
}
