using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Интерфейс генератора программных проектов
/// </summary>
public interface ISoftwareProjectGenerator
{
    /// <summary>
    /// Генерирует программный проект с указанным идентификатором
    /// </summary>
    public SoftwareProject Generate(int id);
}
