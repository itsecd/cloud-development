using Bogus;

namespace Contracts;

public static class EmployeeGenerator
{
    private static readonly string[] PositionPrefixes =
        { "Junior", "Middle", "Senior", "Lead", "Principal" };

    private static readonly string[] PositionTitles =
        { "Developer", "Manager", "Analyst", "Designer", "Tester", "Architect", "DevOps Engineer" };

    private static readonly Dictionary<string, decimal[]> SalaryRanges = new()
    {
        { "Junior",    new[] { 30_000.00m,  60_000.00m } },
        { "Middle",    new[] { 60_000.00m, 100_000.00m } },
        { "Senior",    new[] { 100_000.00m, 160_000.00m } },
        { "Lead",      new[] { 150_000.00m, 220_000.00m } },
        { "Principal", new[] { 200_000.00m, 300_000.00m } }
    };

    public static List<Employee> Generate(int count)
    {
        var id = 1;

        var faker = new Faker<Employee>("ru")
            .RuleFor(e => e.Id, _ => id++)
            .RuleFor(e => e.FullName, f =>
                f.Name.LastName() + " " + f.Name.FirstName() + " " + f.Name.FirstName())
            .RuleFor(e => e.Position, f =>
            {
                var prefix = f.PickRandom(PositionPrefixes);
                var title = f.PickRandom(PositionTitles);
                return prefix + " " + title;
            })
            .RuleFor(e => e.Department, f => f.Commerce.Department())
            .RuleFor(e => e.HireDate, f =>
                DateOnly.FromDateTime(f.Date.Past(10).Date))
            .RuleFor(e => e.Salary, (f, e) =>
            {
                var prefix = e.Position.Split(' ')[0];
                decimal min = 30_000.00m;
                decimal max = 60_000.00m;
                if (SalaryRanges.TryGetValue(prefix, out var range))
                {
                    min = range[0];
                    max = range[1];
                }
                return Math.Round(f.Random.Decimal(min, max), 2);
            })
            .RuleFor(e => e.Email, f => f.Internet.Email())
            .RuleFor(e => e.PhoneNumber, f =>
            {
                var digits = f.Random.Digits(10);
                return "+7(" + digits[0] + digits[1] + digits[2] + ")"
                    + digits[3] + digits[4] + digits[5]
                    + "-" + digits[6] + digits[7]
                    + "-" + digits[8] + digits[9];
            })
            .RuleFor(e => e.IsDismissed, f => f.Random.Bool(0.2f))
            .RuleFor(e => e.DismissalDate, (f, e) =>
            {
                if (!e.IsDismissed) return null;
                var hireDateTime = e.HireDate.ToDateTime(TimeOnly.MinValue);
                var dismissDate = f.Date.Between(hireDateTime, DateTime.Now);
                return DateOnly.FromDateTime(dismissDate);
            });

        return faker.Generate(count);
    }
}
