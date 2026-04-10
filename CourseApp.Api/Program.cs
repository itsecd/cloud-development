using CourseApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<ICourseService, CourseService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/courses", async (int id, ICourseService courseService) =>
    Results.Ok(await courseService.GetCourse(id)));

app.Run();
