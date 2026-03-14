using System.Threading;
using System.Threading.Tasks;
using Patient.Generator.DTO;

namespace Patient.Generator.Service;

/// <summary>
/// Интерфейс для кэширования медицинских пациентов.
/// </summary>
public interface IPatientCache
{
    /// <summary>
    /// Получить пациента из кэша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO пациента или null, если не найден в кэше.</returns>
    public Task<PatientDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить пациента в кэш.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="value">DTO пациента для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public Task SetAsync(int id, PatientDto value, CancellationToken cancellationToken = default);
}
