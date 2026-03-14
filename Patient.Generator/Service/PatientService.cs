using System.Threading;
using System.Threading.Tasks;
using Patient.Generator.DTO;
using Patient.Generator.Generator;

namespace Patient.Generator.Service;

/// <summary>
/// Реализация сервиса работы с медицинскими пациентами.
/// </summary>
public sealed class PatientService(
    PatientGenerator generator,
    IPatientCache cache) : IPatientService
{
    /// <summary>
    /// Получить пациента по идентификатору. Если пациент не найден в кэше, генерирует нового и сохраняет в кэш.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO пациента.</returns>
    public async Task<PatientDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync(id, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var generated = generator.Generate(id);
        await cache.SetAsync(id, generated, cancellationToken);

        return generated;
    }
}
