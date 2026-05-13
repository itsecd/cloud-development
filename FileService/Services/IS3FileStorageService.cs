using Domain.Contracts;

namespace FileService.Services;

public interface IS3FileStorageService
{
    Task<string> SaveContractAsync(VehicleContractDto contract);
    Task EnsureBucketExistsAsync();
}
