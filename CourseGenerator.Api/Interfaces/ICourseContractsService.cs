using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Interfaces;

public interface ICourseContractsService
{
    Task<IReadOnlyList<CourseContract>> GenerateAsync(int count, CancellationToken cancellationToken = default);
}
