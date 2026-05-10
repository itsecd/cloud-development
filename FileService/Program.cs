using File.Service.Background;
using File.Service.Configuration;
using File.Service.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.Configure<AwsStorageOptions>(builder.Configuration.GetSection(AwsStorageOptions.SectionName));

builder.Services.AddSingleton<FileExportInfrastructureState>();
builder.Services.AddSingleton<IEmployeeFileStorage, S3EmployeeFileStorage>();
builder.Services.AddHostedService<SnsSqsFileExportWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "File.Service",
    description = "Файловый сервис, сохраняющий сведения о сотрудниках в объектное хранилище",
    endpoints = new[] { "/ready", "/files/{id}" }
}));

app.MapGet("/ready", (FileExportInfrastructureState state) =>
{
    return state.IsInitialized
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/files/{id:int}", async (int id, IEmployeeFileStorage storage, CancellationToken cancellationToken) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { message = "Идентификатор сотрудника должен быть больше нуля." });
    }

    var content = await storage.TryReadEmployeeJsonAsync(id, cancellationToken);
    if (string.IsNullOrWhiteSpace(content))
    {
        return Results.NotFound(new { message = $"Файл для сотрудника {id} не найден." });
    }

    return Results.Text(content, "application/json");
});

app.Run();

public partial class Program;