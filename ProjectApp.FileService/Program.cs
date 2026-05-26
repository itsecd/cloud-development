using Minio;
using ProjectApp.FileService.Messaging;
using ProjectApp.FileService.ObjectStorage;
using ProjectApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var objectStorageOptions = builder.Configuration
    .GetSection(ObjectStorageOptions.SectionName)
    .Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();

builder.Services.AddSingleton(objectStorageOptions);
builder.Services.AddSingleton<IMinioClient>(_ =>
{
    var endpoint = NormalizeEndpoint(objectStorageOptions.Endpoint);
    return new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(objectStorageOptions.AccessKey, objectStorageOptions.SecretKey)
        .WithSSL(objectStorageOptions.UseSsl)
        .Build();
});
builder.Services.AddSingleton<IPatientFileStorage, MinioPatientFileStorage>();
builder.Services.AddHostedService<PatientGeneratedConsumer>();

var app = builder.Build();

app.MapGet("/api/files/patients/{id:int}", async (int id, IPatientFileStorage storage, CancellationToken cancellationToken) =>
{
    var json = await storage.GetPatientJsonAsync(id, cancellationToken);
    return json is null
        ? Results.NotFound()
        : Results.Text(json, "application/json");
});

app.MapDefaultEndpoints();

app.Run();

static string NormalizeEndpoint(string endpoint)
{
    if (endpoint.Contains("://", StringComparison.Ordinal) &&
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
    {
        return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    }

    return endpoint;
}
