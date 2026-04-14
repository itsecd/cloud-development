using Generator.DTO;
using Generator.Generator;
using Generator.Service.Cache;
using Generator.Service.Messaging;

namespace Generator.Service;

/// <summary>
/// Реализация <see cref="IResidentialBuildingService"/>.
/// </summary>
public class ResidentialBuildingService(
    ILogger<ResidentialBuildingService> logger,
    ResidentialBuildingGenerator generator,
    ICacheService cacheService,
    IPublisherService messagingService
    ) : IResidentialBuildingService
{
    
    /// <inheritdoc />
    public async Task<ResidentialBuildingDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var obj = await cacheService.GetCache<ResidentialBuildingDto>(id, cancellationToken);

        if (obj is not null)
        {
            return obj;
        }
        
        obj = generator.Generate(id);
        await cacheService.SetCache(id, obj, cancellationToken);
        await messagingService.SendMessage(obj);
        
        logger.LogInformation("Generated, cached and sent to SNS residential building with Id={id}", id);
        return obj;
    }
}