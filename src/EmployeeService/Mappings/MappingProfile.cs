using AutoMapper;
using EmployeeService.DTOs;
using EmployeeService.Models;

namespace EmployeeService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Employee, EmployeeDto>();
        CreateMap<CreateEmployeeDto, Employee>();
    }
}
