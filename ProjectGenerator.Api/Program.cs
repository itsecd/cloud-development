using ProjectGenerator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddSingleton<ISoftwareProjectGenerator, SoftwareProjectGenerator>();
builder.Services.AddScoped<ISoftwareProjectService, SoftwareProjectService>();

var trustedUrls = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(trustedUrls)
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

app.MapGet("/generate", async (int id, ISoftwareProjectService service) =>
    await service.GetOrGenerate(id));

app.Run();
