using System.Text.Json;
using CompanyEmployee.DtoModel;
using CompanyEmployee.FileService.Services;
using MassTransit;

namespace CompanyEmployee.FileService.Consumers;

public class CompanyEmployeeConsumer : IConsumer<ModelDTO>
{
    private readonly MinioService _minio;

    public CompanyEmployeeConsumer(MinioService minio)
    {
        _minio = minio;
    }

    public async Task Consume(ConsumeContext<ModelDTO> context)
    {
        var employee = context.Message;
        var fileName = $"employee-{employee.Id}.json";

        await _minio.UploadJsonAsync(fileName, JsonSerializer.Serialize(employee));
    }
}