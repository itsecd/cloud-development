namespace CourseGenerator.Api.Dto;

/// <summary>
/// Query-параметры генерации списка учебных контрактов.
/// </summary>
public sealed class CourseGenerationQueryDto
{
    /// <summary>
    /// Количество контрактов для генерации. Допустимый диапазон: от 1 до 100.
    /// </summary>
    public int Count { get; set; }
}
