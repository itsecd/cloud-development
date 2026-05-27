using Bogus;

namespace ProjectApp.Api.Function;

public class CreditApplicationGenerator
{
    private readonly Faker<CreditApplication> _faker = CreateFaker();

    public CreditApplication Generate(int id)
    {
        var application = _faker.Generate();
        application.Id = id;
        return application;
    }

    private static Faker<CreditApplication> CreateFaker()
    {
        var creditTypes = new[]
        {
            "Потребительский",
            "Ипотека",
            "Автокредит",
            "Рефинансирование",
            "Кредитная карта"
        };
        var nonTerminalStatuses = new[] { "Новая", "В обработке" };
        var terminalStatuses = new[] { "Одобрена", "Отклонена" };
        var minApplicationDate = DateTime.Today.AddYears(-2);
        var maxApplicationDate = DateTime.Today.AddDays(-1);

        return new Faker<CreditApplication>("ru")
            .RuleFor(c => c.CreditType, f => f.PickRandom(creditTypes))
            .RuleFor(c => c.RequestedAmount, f => Math.Round(f.Finance.Amount(50000, 5000000), 2))
            .RuleFor(c => c.TermMonths, f => f.Random.Int(6, 360))
            .RuleFor(c => c.InterestRate, f => Math.Round(f.Random.Double(21.0, 33.0), 2))
            .RuleFor(c => c.ApplicationDate, f => DateOnly.FromDateTime(f.Date.Between(minApplicationDate, maxApplicationDate)))
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.Random.Bool(0.7f)
                ? f.PickRandom(terminalStatuses)
                : f.PickRandom(nonTerminalStatuses))
            .RuleFor(c => c.DecisionDate, (f, c) =>
            {
                if (c.Status is not ("Одобрена" or "Отклонена"))
                {
                    return null;
                }

                var minDate = c.ApplicationDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
                var maxDate = DateTime.Today;
                return DateOnly.FromDateTime(f.Date.Between(minDate, maxDate));
            })
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
            {
                if (c.Status != "Одобрена")
                {
                    return null;
                }

                return Math.Round(f.Finance.Amount(50000, c.RequestedAmount), 2);
            });
    }
}
