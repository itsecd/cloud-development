namespace MedicalPatient.Generator;

public record MedicalPatientMessage(
    int Id,
    string FullName,
    string Address,
    DateOnly BirthDate,
    double Height,
    double Weight,
    int BloodType,
    bool RhFactor,
    DateOnly LastInspectionDate,
    bool VaccinationMark
);
