namespace CompanyEmployee.DtoModel;

public record ModelDTO(
    int Id,
    string FullName,
    string JobTitle,
    string Department,
    DateOnly AdmissionDate,
    decimal Salary,
    string Email,
    string PhoneNumber,
    bool Dismissal,
    DateOnly? DismissalDate
);