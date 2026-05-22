using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using File.Service.Background;
using File.Service.Configuration;
using File.Service.Sns;
using File.Service.Storage;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.Configure<AwsStorageOptions>(builder.Configuration.GetSection(AwsStorageOptions.SectionName));

// Регистрируем AWS-клиентов через DI, чтобы они не создавались вручную внутри сервисов
// и пользовались LocalStack-эндпойнтом из конфигурации.
builder.Services.AddDefaultAWSOptions(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AwsStorageOptions>>().Value;
    var awsOptions = new AWSOptions
    {
        Region = RegionEndpoint.GetBySystemName(opts.Region),
        Credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey)
    };
    awsOptions.DefaultClientConfig.ServiceURL = opts.ServiceUrl;
    awsOptions.DefaultClientConfig.AuthenticationRegion = opts.Region;
    awsOptions.DefaultClientConfig.UseHttp = opts.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    return awsOptions;
});

builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// AWSOptions.DefaultClientConfig не содержит ForcePathStyle, необходимого для LocalStack S3,
// поэтому собираем клиента отдельной фабрикой, но он по-прежнему резолвится из DI.
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AwsStorageOptions>>().Value;
    return new AmazonS3Client(
        new BasicAWSCredentials(opts.AccessKey, opts.SecretKey),
        new AmazonS3Config
        {
            ServiceURL = opts.ServiceUrl,
            AuthenticationRegion = opts.Region,
            ForcePathStyle = true,
            UseHttp = opts.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        });
});

builder.Services.AddSingleton<FileExportInfrastructureState>();
builder.Services.AddSingleton<IEmployeeFileStorage, S3EmployeeFileStorage>();
builder.Services.AddSingleton<ISnsNotificationHandler, SnsNotificationHandler>();
builder.Services.AddHttpClient(SnsNotificationHandler.ConfirmationHttpClientName);
builder.Services.AddHostedService<SnsFileExportWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "File.Service",
    description = "Файловый сервис, сохраняющий сведения о сотрудниках в объектное хранилище",
    endpoints = new[] { "/ready", "/files/{id}", "POST /sns/notifications" }
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

app.MapPost("/sns/notifications", (
    HttpContext context,
    ISnsNotificationHandler handler,
    CancellationToken cancellationToken) => handler.HandleAsync(context, cancellationToken));

app.Run();

public partial class Program;
