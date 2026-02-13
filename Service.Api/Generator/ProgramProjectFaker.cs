using Bogus;
using Service.Api.Entity;

namespace Service.Api.Generator;

public class ProgramProjectFaker : Faker<ProgramProject>
{
    public ProgramProjectFaker() : base("ru")
    {
        RuleFor(o => o.Name, f => f.Commerce.ProductName());
        RuleFor(o => o.Customer, f => f.Company.CompanyName());
        RuleFor(o => o.Manager, f => f.Name.FullName());
        RuleFor(o => o.StartDate, f => DateOnly.FromDateTime(f.Date.Past(2, DateTime.Now)));
        RuleFor(o => o.EndDatePlanned, (f, o) => DateOnly.FromDateTime(f.Date.Future(5, o.StartDate.ToDateTime(TimeOnly.MinValue))));
        RuleFor(o => o.EndDateReal, (f, o) =>
        {
            DateTime end = f.Date.Between(o.StartDate.ToDateTime(TimeOnly.MinValue), o.EndDatePlanned.ToDateTime(TimeOnly.MinValue));
            return end > DateTime.Now ? null : DateOnly.FromDateTime(end);
        });
        RuleFor(o => o.Budget, f => Math.Round(f.Finance.Amount(100_000, 1_000_000), 2));
        RuleFor(o => o.FinishedPerCent, (f, o) => o.EndDateReal != null ? 100 : f.Random.Number(1, 100));
        RuleFor(o => o.SpentMoney, (f, o) =>
        {
            var spread = Convert.ToInt32(o.Budget) / 15;
            return Math.Round((o.Budget - f.Finance.Amount(-spread, spread)) * o.FinishedPerCent / 100, 2);
        });
    }
}
