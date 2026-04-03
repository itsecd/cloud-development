namespace CompanyEmployee.FileService.Services;

public interface IS3FileStorage
{
    public Task UploadFileAsync(string bucketName, string key, byte[] content);
}