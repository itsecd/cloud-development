using FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<MinioStorageService>();
builder.Services.AddHostedService<SqsPollingService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/files", async (MinioStorageService storage, CancellationToken ct) =>
    Results.Ok(await storage.ListFilesAsync(ct)));

app.Run();
