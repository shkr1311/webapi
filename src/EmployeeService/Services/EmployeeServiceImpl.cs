using AutoMapper;
using EmployeeService.DTOs;
using EmployeeService.Models;
using EmployeeService.Repositories;

namespace EmployeeService.Services;

public class EmployeeServiceImpl : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeServiceImpl> _logger;

    public EmployeeServiceImpl(IEmployeeRepository repository, IMapper mapper, ILogger<EmployeeServiceImpl> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
    {
        var employees = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _repository.GetByIdAsync(id);
        if (employee == null)
        {
            _logger.LogWarning("Employee with ID {EmployeeId} not found", id);
            return null;
        }
        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto?> GetFirstAvailableEmployeeAsync()
    {
        var employee = await _repository.GetFirstAvailableAsync();
        if (employee == null)
        {
            _logger.LogWarning("No available employees found");
            return null;
        }
        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var employee = _mapper.Map<Employee>(dto);
        var created = await _repository.CreateAsync(employee);
        _logger.LogInformation("Employee created successfully with ID: {EmployeeId}", created.Id);
        return _mapper.Map<EmployeeDto>(created);
    }

    public async Task<bool> UpdateAvailabilityAsync(int id, bool isAvailable)
    {
        var result = await _repository.UpdateAvailabilityAsync(id, isAvailable);
        if (!result)
            _logger.LogWarning("Cannot update availability - Employee with ID {EmployeeId} not found", id);
        return result;
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
}
