using Generator.DTO;

namespace Generator.Service;

/// <summary>
///     Сервис получения объектов жилого строительства по идентификатору.
///     Если удалось найти объект в кэше - возвращает его, иначе генерирует, кэширует и возвращает сгенерированный.
/// </summary>
public interface IResidentialBuildingService
{
    /// <summary>
    ///     Пытается найти в кэше объект с заданным идентификатором:
    ///     если удалось, то десериализует объект из JSON-а и возвращает;
    ///     если не удалось или произошла ошибка в ходе получения/десериализации, то генерирует объект, сохраняет в кэш и
    ///     возвращает сгенерированный.
    /// </summary>
    /// <param name="id">Идентификатор объекта жилого строительства.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO объекта жилого строительства.</returns>
    public Task<ResidentialBuildingDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}