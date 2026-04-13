namespace CompanyEmployees.FileService.Models;

public record EmployeeMessage(
    int Id,
    string FullName,
    string Position,
    string Section,
    DateOnly AdmissionDate,
    decimal Salary,
    string Email,
    string PhoneNumber,
    bool Dismissal,
    DateOnly? DismissalDate
);
