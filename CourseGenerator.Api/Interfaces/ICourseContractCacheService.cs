using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Interfaces;

public interface ICourseContractCacheService
{
    Task<IReadOnlyList<CourseContract>?> GetAsync(int count, CancellationToken cancellationToken = default);
    Task SetAsync(int count, IReadOnlyList<CourseContract> contracts, CancellationToken cancellationToken = default);
}
