using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Options;
using Service.Api.Configuration;
using Service.Api.Generator;
using Service.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.Configure<AwsMessagingOptions>(builder.Configuration.GetSection(AwsMessagingOptions.SectionName));

// Регистрируем AWS-клиентов через DI, чтобы они не создавались вручную внутри сервисов
// и пользовались LocalStack-эндпойнтом из конфигурации.
builder.Services.AddDefaultAWSOptions(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AwsMessagingOptions>>().Value;
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

builder.Services.AddSingleton<IEmployeeEventPublisher, SnsEmployeeEventPublisher>();
builder.Services.AddScoped<IEmployeeGeneratorService, EmployeeGeneratorService>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.SetIsOriginAllowed(origin =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.IsLoopback
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .AllowAnyHeader()
        .WithMethods("GET")
        .WithExposedHeaders("X-Service-Replica", "X-Service-Weight");
}));

var app = builder.Build();

var replicaId = app.Configuration["ReplicaId"] ?? Environment.MachineName;
var replicaWeight = app.Configuration.GetValue<int?>("ReplicaWeight") ?? 1;

app.UseCors();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Service-Replica"] = replicaId;
    context.Response.Headers["X-Service-Weight"] = replicaWeight.ToString();
    await next();
});

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "Service.Api",
    replica = replicaId,
    weight = replicaWeight,
    description = "Сервис генерации сотрудников компании",
    endpoints = new[] { "/employee?id=1", "/employee/1" }
}));

app.MapGet("/employee", async (IEmployeeGeneratorService service, ILoggerFactory loggerFactory, int id, CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("ServiceApiEndpoints");
    logger.LogInformation("Replica {ReplicaId} received request for employee {EmployeeId}", replicaId, id);

    if (id <= 0)
    {
        return Results.BadRequest(new { message = "Идентификатор сотрудника должен быть больше нуля." });
    }

    return Results.Ok(await service.ProcessEmployee(id, cancellationToken));
});

app.MapGet("/employee/{id:int}", async (IEmployeeGeneratorService service, ILoggerFactory loggerFactory, int id, CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("ServiceApiEndpoints");
    logger.LogInformation("Replica {ReplicaId} received request for employee {EmployeeId}", replicaId, id);

    if (id <= 0)
    {
        return Results.BadRequest(new { message = "Идентификатор сотрудника должен быть больше нуля." });
    }

    return Results.Ok(await service.ProcessEmployee(id, cancellationToken));
});

app.Run();

public partial class Program;
