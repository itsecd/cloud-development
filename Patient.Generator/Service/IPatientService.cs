using System.Threading;
using System.Threading.Tasks;
using Patient.Generator.DTO;

namespace Patient.Generator.Service;

/// <summary>
/// Интерфейс для сервиса работы с медицинскими пациентами.
/// </summary>
public interface IPatientService
{
    /// <summary>
    /// Получить пациента по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO пациента.</returns>
    public Task<PatientDto> GetAsync(int id, CancellationToken cancellationToken = default);
}
