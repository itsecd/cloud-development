namespace PatientApp.Generator.Models;

public class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public DateOnly Birthday { get; set; }
    public string Address { get; set; }

    public double Height { get; set; }
    public double Weight { get; set; }

    public int BloodType { get; set; }

    public bool Resus {  get; set; }

    public DateOnly LastVisit { get; set; }

    public bool Vactination { get; set; }

}
