using Bogus;
using CreditApp.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CreditApp.Api.Services.CreditGeneratorService;

public class CreditApplicationGeneratorService(IDistributedCache _cache, IConfiguration _configuration, ILogger<CreditApplicationGeneratorService> _logger)
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

    private readonly int _expirationMinutes = _configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    public async Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"credit-application-{id}";

            _logger.LogInformation("Попытка получить заявку {Id} из кэша", id);

            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var deserializedApplication = JsonSerializer.Deserialize<CreditApplication>(cachedData);

                if (deserializedApplication != null)
                {
                    _logger.LogInformation("Заявка {Id} найдена в кэше", id);
                    return deserializedApplication;
                }

                _logger.LogWarning("Заявка {Id} найдена в кэше, но не удалось десериализовать. Генерируем новую", id);
            }

            _logger.LogInformation("Заявка {Id} не найдена в кэше, генерируем новую", id);

            var application = GenerateApplication(id);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expirationMinutes)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении/генерации заявки {Id}", id);
            throw;
        }
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
            .RuleFor(c => c.SubmissionDate, f => f.Date.PastDateOnly(2))
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.PickRandom(_statuses))
            .RuleFor(c => c.ApprovalDate, (f, c) =>
            {
                if (!_terminalStatuses.Contains(c.Status))
                    return null;

                return f.Date.BetweenDateOnly(c.SubmissionDate, DateOnly.FromDateTime(DateTime.Today));
            })
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
            {
                if (c.Status != "Одобрена")
                    return null;

                return Math.Round(c.Amount * f.Random.Decimal(0.7m, 1.0m), 2);
            });

        return faker.Generate();
    }
}
