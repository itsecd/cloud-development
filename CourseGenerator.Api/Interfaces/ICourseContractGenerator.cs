using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Interfaces;

public interface ICourseContractGenerator
{
    IReadOnlyList<CourseContract> Generate(int count);
}
