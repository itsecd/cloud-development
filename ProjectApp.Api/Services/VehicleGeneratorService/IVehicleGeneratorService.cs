using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.VehicleGeneratorService;

/// <summary>
/// Интерфейс сервиса получения характеристик машины
/// </summary>
public interface IVehicleGeneratorService
{
    /// <summary>
    /// Запрашивает машину по ID
    /// </summary>
    public Task<Vehicle> FetchByIdAsync(int id, CancellationToken cancellationToken = default);
}
