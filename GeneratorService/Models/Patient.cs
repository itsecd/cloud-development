namespace PatientApp.Models;

public class Patient
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public string Surname { get; set; }

    public string Patronymic { get; set; }
    public DateOnly Birthday { get; set; }
    public Gender  Gender { get; set; }

    public string Diagnosis { get; set; }

    public string Address { get; set; }

}
