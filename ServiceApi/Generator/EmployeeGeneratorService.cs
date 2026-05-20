using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Service.Api.Entities;
using Service.Api.Messaging;

namespace Service.Api.Generator;

public sealed class EmployeeGeneratorService(
    IDistributedCache cache,
    ILogger<EmployeeGeneratorService> logger,
    IConfiguration configuration,
    IEmployeeEventPublisher eventPublisher) : IEmployeeGeneratorService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TimeSpan _cacheExpiration =
        TimeSpan.FromMinutes(configuration.GetValue<int?>("CacheExpirationMinutes") ?? 30);

    public async Task<Employee> ProcessEmployee(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var cacheKey = $"employee:{id}";
        var fromCache = false;

        using var _ = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EmployeeId"] = id,
            ["CacheKey"] = cacheKey
        });

        logger.LogInformation("Employee request received");

        try
        {
            var employee = await RetrieveFromCache(cacheKey, cancellationToken);
            if (employee is not null)
            {
                fromCache = true;
                logger.LogInformation("Cache hit. Returning employee from Redis");
                return employee;
            }

            logger.LogInformation("Cache miss. Generating new employee");
            employee = EmployeeGenerator.Generate(id);
            await PopulateCache(cacheKey, employee, cancellationToken);
            await eventPublisher.PublishAsync(employee, cancellationToken);

            logger.LogInformation(
                "Employee {EmployeeId} generated, stored in cache for {CacheLifetimeMinutes} minutes and published to SNS",
                id,
                _cacheExpiration.TotalMinutes);

            return employee;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while processing employee {EmployeeId}. FromCache={FromCache}", id, fromCache);
            throw;
        }
    }

    private async Task<Employee?> RetrieveFromCache(string cacheKey, CancellationToken cancellationToken)
    {
        var json = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Employee>(json, JsonOptions);
    }

    private async Task PopulateCache(string cacheKey, Employee employee, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(employee, JsonOptions);

        await cache.SetStringAsync(
            cacheKey,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            },
            cancellationToken);
    }
}
