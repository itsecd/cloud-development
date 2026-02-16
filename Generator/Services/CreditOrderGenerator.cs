namespace Generator.Services;

using Bogus;
using Generator.Dto;

public class CreditOrderGenerator
{

    private static readonly string[] _сreditTypes = { "Потребительский", "Ипотека", "Автокредит", "Микрозайм" };

    private static readonly string[] _statuses = { "Новая", "В обработке", "Одобрена", "Отклонена" };
    public CreditOrderDto Generate(int id)
    {        
        var faker = new Faker<CreditOrderDto>("ru")
            .RuleFor(x => x.Id, _ => id)
            .RuleFor(x => x.CreditType, f => f.PickRandom(_сreditTypes))
            .RuleFor(x => x.RequestedSum, f => Math.Round(f.Finance.Amount(1_000_000m, 100_000_000m), 2))
            .RuleFor(x => x.MonthsDuration, f => f.Random.Int(1, 360))
            .RuleFor(x => x.InterestRate, f => Math.Round(f.Random.Double(15.6, 20.0), 2))
            .RuleFor(x => x.FilingDate, f => DateOnly.FromDateTime(f.Date.Past(2)))
            .RuleFor(x => x.IsInsuranceNeeded, f => f.Random.Bool())
            .RuleFor(x => x.OrderStatus, f => f.PickRandom(_statuses))
            .RuleFor(x => x.DecisionDate, _ => null)
            .RuleFor(x => x.ApprovedSum, _ => null); 
        
        var order = faker.Generate();

        ApplyDependencies(order);

        return order;
    }

    private static void ApplyDependencies(CreditOrderDto order)
    {

            if(order.OrderStatus is "Одобрена")
            {
                order.DecisionDate = order.FilingDate.AddDays(Random.Shared.Next(0, 31));

                var k = Random.Shared.NextDouble() * 0.4 + 0.6;
                var approved = order.RequestedSum * (decimal)k;
                order.ApprovedSum = Math.Round(approved, 2);
            }
            else if (order.OrderStatus is "Отклонена")
            {
                order.DecisionDate = order.FilingDate.AddDays(Random.Shared.Next(0, 31));
                order.ApprovedSum = null;
            }
            else
            {
                order.DecisionDate = null;
                order.ApprovedSum = null;
            }
        
    }
}
