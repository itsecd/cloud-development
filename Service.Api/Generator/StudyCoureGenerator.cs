using Bogus;
using Service.Api.Entities;
namespace Service.Api.Generator;

public static class StudyCoureGenerator
{
    private static readonly List<string> _courseNames = ["English", "Spanish", "Hand Craft", "Math", "Chinese", "Yoga"];

    private const int MaxRating = 5;
    private const int MinRating = 1;
    private const int DigitsRound = 2;
    private const int MinCost = 0;
    private const int MaxCost = 10000000;
    private const int MinStudents = 0;
    private const int MaxStudents = 1000;
    private static readonly DateOnly _minDate = new (2020, 1, 1);
    private static readonly DateOnly _maxDate = new(2030, 1, 1);
    private const int MaxYearsForward = 5;

    private static Faker<StudyCourse> _faker = new Faker<StudyCourse>("en")
        .RuleFor(s => s.TeacherFullName, f => $"{f.PickRandom(f.Person.FirstName)} {f.PickRandom(f.Person.LastName)}")
        .RuleFor(s => s.CourseName, f => f.PickRandom(_courseNames))
        .RuleFor(s => s.StartDate, f => f.Date.BetweenDateOnly(_minDate, _maxDate))
        .RuleFor(s => s.EndDate, (f, s) => f.Date.FutureDateOnly(MaxYearsForward, s.StartDate))
        .RuleFor(s => s.GivesCertificate, f => f.Random.Bool())
        .RuleFor(s => s.Cost, f => Math.Round(f.Random.Decimal(MinCost, MaxCost), DigitsRound))
        .RuleFor(s => s.MaxStudents, f => f.Random.Int(MinStudents, MaxStudents))
        .RuleFor(s => s.CurrentStudents, (f, s) => f.Random.Int(MinStudents, s.MaxStudents))
        .RuleFor(s => s.Rating, f => f.Random.Int(MinRating, MaxRating));

    public static StudyCourse GenerateCourse(int id)
    {
        var course = _faker.Generate();
        course.Id = id;
        return course;
    }

}
