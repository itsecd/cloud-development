using SoftwareProjects.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<ISoftwareProjectCacheService, SoftwareProjectCacheService>();
builder.Services.AddScoped<ISoftwareProjectService, SoftwareProjectService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/software-projects", async (int id, ISoftwareProjectService service) =>
    Results.Ok(await service.GetById(id)));

app.Run();
