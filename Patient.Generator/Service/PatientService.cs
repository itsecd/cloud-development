using Patient.Generator.DTO;
using Patient.Generator.Generator;
using Patient.Generator.Messaging;

namespace Patient.Generator.Service;

/// <summary>
/// Реализация сервиса работы с медицинскими пациентами.
/// </summary>
/// <param name="generator">Генератор пациентов.</param>
/// <param name="cache">Кэш пациентов.</param>
/// <param name="producer">Служба отправки пациентов в брокер сообщений.</param>
public sealed class PatientService(
    PatientGenerator generator,
    IPatientCache cache,
    IProducerService producer) : IPatientService
{
    /// <summary>
    /// Получить пациента по идентификатору. Если пациент не найден в кэше, генерирует нового,
    /// сохраняет в кэш и отправляет в брокер сообщений.
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
        await producer.SendMessage(generated);

        return generated;
    }
}
