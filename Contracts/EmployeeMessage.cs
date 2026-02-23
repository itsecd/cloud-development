namespace Contracts;

public class EmployeeMessage
{
    public string Action { get; set; } = "generated";
    public List<Employee> Employees { get; set; } = new();
}
