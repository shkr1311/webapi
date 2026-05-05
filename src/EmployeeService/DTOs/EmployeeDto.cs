namespace EmployeeService.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class UpdateEmployeeDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
}

public class AvailabilityDto
{
    public bool IsAvailable { get; set; }
}
