namespace CreditApp.FileService;

/// <summary>
/// Интерфейс для работы с файловым хранилищем
/// </summary>
public interface IFileStorage
{
    public Task SaveAsync(string bucketName, string fileName, byte[] data, CancellationToken cancellationToken = default);

    public Task<byte[]?> GetAsync(string bucketName, string fileName, CancellationToken cancellationToken = default);
}