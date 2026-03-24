using CreditApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<CreditApplicationGenerator>();
builder.Services.AddScoped<ICreditApplicationService, CreditApplicationService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/credit-application", async (int id, ICreditApplicationService service) =>
    await service.GetOrGenerate(id));

app.Run();
