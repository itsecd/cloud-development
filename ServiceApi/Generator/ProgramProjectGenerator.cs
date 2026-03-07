using Bogus;
using ServiceApi.Entities;

namespace ServiceApi.Generator;

/// <summary>
/// Генератор программных проектов со случайными свойствами
/// </summary>
public static class ProgramProjectGenerator
{
    private static readonly Faker<ProgramProject> _faker = new Faker<ProgramProject>("ru")
        .RuleFor(o => o.Name, f =>
        f.Lorem.Word() + " " +
        f.Hacker.Abbreviation())
        .RuleFor(o => o.Customer, f => f.Company.CompanyName())
        .RuleFor(o => o.Manager, f => f.Name.FullName())
        .RuleFor(o => o.StartDate, f => DateOnly.FromDateTime(f.Date.Past(3, DateTime.Now)))
        .RuleFor(o => o.PlanEndDate, (f, o) => DateOnly.FromDateTime(f.Date.Future(3, o.StartDate.ToDateTime(TimeOnly.MinValue))))
        .RuleFor(o => o.ActualEndDate, (f, o) =>
        {
            DateTime end = f.Date.Between(o.StartDate.ToDateTime(TimeOnly.MinValue), o.PlanEndDate.ToDateTime(TimeOnly.MinValue));
            return end > DateTime.Now ? null : DateOnly.FromDateTime(end);
        })
        .RuleFor(o => o.Budget, f => Math.Round(f.Finance.Amount(100000, 10000000), 2))
        .RuleFor(o => o.PercentComplete, (f, o) => o.ActualEndDate != null ? 100 : f.Random.Number(0, 99))
        .RuleFor(o => o.ActualCost, (f, o) =>
        {
            var scatter = Convert.ToInt32(o.Budget) / 10;
            return Math.Round((o.Budget - f.Finance.Amount(-scatter, scatter)) * o.PercentComplete / 100, 2);
        });

        /// <summary>
        /// Метод генерации ПП
        /// </summary>
        /// <param name="id">Идентификатор ПП</param>
        /// <returns>Программный проект</returns>
    public static ProgramProject GenerateProgramProject(int id)
    {
        var project = _faker.Generate();
        project.Id = id;
        return project;
    }
}