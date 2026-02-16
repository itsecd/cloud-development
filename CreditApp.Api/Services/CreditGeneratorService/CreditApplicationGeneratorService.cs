using Bogus;
using CreditApp.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CreditApp.Api.Services.CreditGeneratorService;

public class CreditApplicationGeneratorService(IDistributedCache _cache, ILogger<CreditApplicationGeneratorService> _logger) : ICreditApplicationGeneratorService
{
    private static readonly string[] _creditTypes = 
    [
        "Потребительский", 
        "Ипотека", 
        "Автокредит", 
        "Бизнес-кредит",
        "Образовательный"
    ];

    private static readonly string[] _statuses = 
    [
        "Новая", 
        "В обработке", 
        "Одобрена", 
        "Отклонена"
    ];

    private static readonly string[] _terminalStatuses = ["Одобрена", "Отклонена"];

    public async Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"credit-application-{id}";
        
        _logger.LogInformation("Попытка получить заявку {Id} из кэша", id);

        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (cachedData != null)
        {
            _logger.LogInformation("Заявка {Id} найдена в кэше", id);
            return JsonSerializer.Deserialize<CreditApplication>(cachedData)!;
        }

        _logger.LogInformation("Заявка {Id} не найдена в кэше, генерируем новую", id);
        var application = GenerateApplication(id);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(application), 
            cacheOptions,
            cancellationToken);

        _logger.LogInformation(
            "Кредитная заявка сгенерирована и закэширована: Id={Id}, Тип={Type}, Сумма={Amount}, Статус={Status}", 
            application.Id, 
            application.Type, 
            application.Amount, 
            application.Status);

        return application;
    }

    /// <summary>
    /// Генерация кредитной заявки с указанным ID
    /// </summary>
    private static CreditApplication GenerateApplication(int id)
    {
        var faker = new Faker<CreditApplication>("ru")
            .RuleFor(c => c.Id, f => id)
            .RuleFor(c => c.Type, f => f.PickRandom(_creditTypes))
            .RuleFor(c => c.Amount, f => Math.Round(f.Finance.Amount(10000, 10000000), 2))
            .RuleFor(c => c.Term, f => f.Random.Int(6, 360))
            .RuleFor(c => c.InterestRate, f => Math.Round(f.Random.Double(16.0, 25.0), 2))
            .RuleFor(c => c.SubmissionDate, f => DateOnly.FromDateTime(
                f.Date.Between(DateTime.Now.AddYears(-2), DateTime.Now)))
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.PickRandom(_statuses));

        var application = faker.Generate();

        if (_terminalStatuses.Contains(application.Status))
        {
            var submissionDate = application.SubmissionDate.ToDateTime(TimeOnly.MinValue);
            var approvalDate = submissionDate.AddDays(new Random().Next(1, 60));
            
            if (approvalDate > DateTime.Now)
            {
                approvalDate = DateTime.Now;
            }
            
            application.ApprovalDate = DateOnly.FromDateTime(approvalDate);
        }

        if (application.Status == "Одобрена")
        {
            var percentage = 0.8m + (decimal)new Random().NextDouble() * 0.3m;
            var approvedAmount = application.Amount * percentage;
            application.ApprovedAmount = Math.Round(approvedAmount, 2);
            
            if (application.ApprovedAmount > application.Amount)
            {
                application.ApprovedAmount = application.Amount;
            }
        }

        return application;
    }
}
