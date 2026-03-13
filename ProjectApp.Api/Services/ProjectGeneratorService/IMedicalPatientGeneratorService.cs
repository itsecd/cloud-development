using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.ProjectGeneratorService;

/// <summary>
/// Сервис получения медицинского пациента
/// </summary>
public interface IMedicalPatientGeneratorService
{
    public Task<MedicalPatient> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
