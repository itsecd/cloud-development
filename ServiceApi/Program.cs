using Service.Api.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.AddScoped<IEmployeeGeneratorService, EmployeeGeneratorService>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.SetIsOriginAllowed(origin =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.IsLoopback
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .AllowAnyHeader()
        .WithMethods("GET");
}));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "Service.Api",
    description = "Сервис генерации сотрудников компании",
    endpoints = new[] { "/employee?id=1", "/employee/1" }
}));

app.MapGet("/employee", async (IEmployeeGeneratorService service, int id, CancellationToken cancellationToken) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { message = "Идентификатор сотрудника должен быть больше нуля." });
    }

    return Results.Ok(await service.ProcessEmployee(id, cancellationToken));
});

app.MapGet("/employee/{id:int}", async (IEmployeeGeneratorService service, int id, CancellationToken cancellationToken) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { message = "Идентификатор сотрудника должен быть больше нуля." });
    }

    return Results.Ok(await service.ProcessEmployee(id, cancellationToken));
});

app.UseCors();

app.Run();
