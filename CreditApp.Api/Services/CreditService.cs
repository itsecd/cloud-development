using CreditApp.Domain.Data;
using CreditApp.Messaging.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CreditApp.Api.Services;

/// <summary>
/// Сервис для работы с кредитными заявками.
/// </summary>
public class CreditService(
    IDistributedCache cache,
    ILogger<CreditService> logger,
    SqsProducer sqsProducer)
    : ICreditService
{
    private const string CachePrefix = "credit:";

    public async Task<CreditApplication> GetAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var key = $"{CachePrefix}{id}";

        var cached = await cache.GetStringAsync(key, cancellationToken);

        if (cached is not null)
        {
            logger.LogInformation(
                "Cache HIT for credit application {CreditId}",
                id);

            return JsonSerializer.Deserialize<CreditApplication>(cached)!;
        }

        logger.LogInformation(
            "Cache MISS for credit application {CreditId}",
            id);

        var result = CreditGenerator.Generate(id);
        var creditEvent = new CreditGeneratedEvent
        {
            Id = result.Id,
            CreditType = result.CreditType,
            RequestedAmount = result.RequestedAmount,
            TermMonths = result.TermMonths,
            InterestRate = result.InterestRate,
            ApplicationDate = result.ApplicationDate,
            HasInsurance = result.HasInsurance,
            Status = result.Status,
            DecisionDate = result.DecisionDate,
            ApprovedAmount = result.ApprovedAmount,
            GeneratedAt = DateTime.UtcNow
        };

        await sqsProducer.PublishAsync(creditEvent);

        var serialized = JsonSerializer.Serialize(result);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await cache.SetStringAsync(key, serialized, options, cancellationToken);


        logger.LogInformation(
            "Generated credit application {CreditId}. Type: {Type}, Amount: {Amount}, Status: {Status}",
            result.Id,
            result.CreditType,
            result.RequestedAmount,
            result.Status);

        return result;
    }
}