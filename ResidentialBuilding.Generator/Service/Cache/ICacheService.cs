namespace Generator.Service.Cache;

public interface ICacheService
{
    public Task<T?> GetCache<T>(int id, CancellationToken cancellationToken = default);
    
    public Task<bool> SetCache<T>(int id, T obj, CancellationToken cancellationToken = default);
}