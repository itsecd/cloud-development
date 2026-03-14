using GeneratorService.Generators;
using Xunit;

namespace GeneratorService.Tests;

public sealed class MedicalPatientGeneratorTests
{
    public static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    public static IEnumerable<object[]> Patients() =>
        Enumerable.Range(1, 300).Select(i => new object[] { MedicalPatientGenerator.Generate(i) });

    [Theory]
    [MemberData(nameof(Patients))]
    public void Id_MatchesRequested(GeneratorService.Models.MedicalPatient p)
        => Assert.True(p.Id > 0);

    [Theory]
    [MemberData(nameof(Patients))]
    public void FullName_HasThreeParts(GeneratorService.Models.MedicalPatient p)
        => Assert.Equal(3, p.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

    [Theory]
    [MemberData(nameof(Patients))]
    public void BirthDate_NotInFuture(GeneratorService.Models.MedicalPatient p)
        => Assert.True(p.BirthDate <= Today);

    [Theory]
    [MemberData(nameof(Patients))]
    public void Height_InReasonableBounds(GeneratorService.Models.MedicalPatient p)
        => Assert.InRange(p.Height, 50.0, 220.0);

    [Theory]
    [MemberData(nameof(Patients))]
    public void Height_RoundedToTwoDecimals(GeneratorService.Models.MedicalPatient p)
        => Assert.Equal(p.Height, Math.Round(p.Height, 2));

    [Theory]
    [MemberData(nameof(Patients))]
    public void Weight_InReasonableBounds(GeneratorService.Models.MedicalPatient p)
        => Assert.InRange(p.Weight, 2.5, 200.0);

    [Theory]
    [MemberData(nameof(Patients))]
    public void Weight_RoundedToTwoDecimals(GeneratorService.Models.MedicalPatient p)
        => Assert.Equal(p.Weight, Math.Round(p.Weight, 2));

    [Theory]
    [MemberData(nameof(Patients))]
    public void BloodGroup_Between1And4(GeneratorService.Models.MedicalPatient p)
        => Assert.InRange(p.BloodGroup, 1, 4);

    [Theory]
    [MemberData(nameof(Patients))]
    public void LastExamination_NotBeforeBirthDate(GeneratorService.Models.MedicalPatient p)
        => Assert.True(p.LastExaminationDate >= p.BirthDate);

    [Theory]
    [MemberData(nameof(Patients))]
    public void LastExamination_NotInFuture(GeneratorService.Models.MedicalPatient p)
        => Assert.True(p.LastExaminationDate <= Today);
}
