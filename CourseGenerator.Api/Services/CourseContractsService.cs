using CourseGenerator.Api.Interfaces;
using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Services;

/// <summary>
/// Прикладной сервис генерации контрактов с использованием кэша.
/// </summary>
public sealed class CourseContractsService(
    ICourseContractGenerator generator,
    ICourseContractCacheService cache,
    ILogger<CourseContractsService> logger) : ICourseContractsService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CourseContract>> GenerateAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and 100.");
        }

        var startedAt = DateTimeOffset.UtcNow;
        var cachedContracts = await cache.GetAsync(count, cancellationToken);

        if (cachedContracts is not null)
        {
            logger.LogInformation(
                "Request processed from cache: {Count}, DurationMs={DurationMs}",
                count,
                (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
            return cachedContracts;
        }

        var contracts = generator.Generate(count);
        await cache.SetAsync(count, contracts, cancellationToken);

        logger.LogInformation(
            "Request processed with generation: {Count}, DurationMs={DurationMs}",
            count,
            (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);

        return contracts;
    }
}
