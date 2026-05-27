using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Реализация сервиса для получения или создания транспортных средств
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly IVehicleGenerator _generator;
    private readonly ICacheService _cache;
    private readonly IConfiguration _configuration;

    public VehicleService(IVehicleGenerator generator, ICacheService cache, IConfiguration configuration)
    {
        _generator = generator;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<Vehicle> GetOrCreateVehicleAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be greater than zero");

        var cached = await _cache.RetrieveVehicleAsync(id);
        if (cached is not null)
            return cached;

        var newVehicle = _generator.CreateVehicle(id);
        var ttl = _configuration.GetValue<int>("Cache:ExpirationMinutes", 5);
        await _cache.StoreVehicleAsync(newVehicle, ttl);

        return newVehicle;
    }
}
