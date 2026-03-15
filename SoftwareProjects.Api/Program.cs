using SoftwareProjects.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<ISoftwareProjectService, SoftwareProjectService>();

var trustedOrigins = builder.Configuration
    .GetSection("TrustedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(trustedOrigins)
              .WithMethods("GET")
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

app.MapGet("/api/software-projects", async (int id, ISoftwareProjectService service) =>
    Results.Ok(await service.GetById(id)));

app.Run();
