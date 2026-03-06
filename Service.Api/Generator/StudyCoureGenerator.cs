using Bogus;
using Service.Api.Entities;
namespace Service.Api.Generator;

public static class StudyCoureGenerator
{
    private static readonly List<string> _firstNames = new(["Donald", "Kanye", "Carl", "Bob", "Alice", "Albert", "Sam", "Andrew"]);

    private static readonly List<string> _secondNames = new(["Smith", "Trump", "Fed", "Stone", "Johnson", "Williams", "Klinton", "Morgan"]);

    private static readonly List<string> _patronymics = new(["Alex", "Bobby", "Steave", "Michael", "John", "Dan", "Ban", "George"]);

    private static readonly List<string> _courseNames = new(["English", "Spanish", "Hand Craft", "Math", "Chinese", "Yoga"]);

    private static readonly int _maxRating = 5;
    private static readonly int _minRating = 1;
    private static readonly int _digitsRound = 2;
    private static readonly int _minCost = 0;
    private static readonly int _maxCost = 10000000;
    private static readonly int _minStudents = 0;
    private static readonly int _maxStudents = 1000;
    private static DateOnly _minDate = new (2020, 1, 1);
    private static DateOnly _maxDate = new(2030, 1, 1);
    private static int _maxYearsForward = 5;

    private static Faker<StudyCourse> _faker = new Faker<StudyCourse>("en")
        .RuleFor(s => s.TeacherFullName, f => $"{f.PickRandom(_firstNames)} {f.PickRandom(_patronymics)} {f.PickRandom(_secondNames)}")
        .RuleFor(s => s.CourseName, f => f.PickRandom(_courseNames))
        .RuleFor(s => s.StartDate, f => f.Date.BetweenDateOnly(_minDate, _maxDate))
        .RuleFor(s => s.EndDate, (f, s) => f.Date.FutureDateOnly(_maxYearsForward, s.StartDate))
        .RuleFor(s => s.GivesCertificate, f => f.Random.Bool())
        .RuleFor(s => s.Cost, f => Math.Round(f.Random.Decimal(_minCost, _maxCost), _digitsRound))
        .RuleFor(s => s.MaxStudents, f => f.Random.Int(_minStudents, _maxStudents))
        .RuleFor(s => s.CurrentStudents, (f, s) => f.Random.Int(_minStudents, s.MaxStudents ?? _maxStudents))
        .RuleFor(s => s.Rating, f => f.Random.Int(_minRating, _maxRating));

    public static StudyCourse GenerateCourse(int id)
    {
        var course = _faker.Generate();
        course.Id = id;
        return course;
    }

}
