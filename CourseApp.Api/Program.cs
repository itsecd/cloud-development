using Amazon.SQS;
using CourseApp.Api.Messaging;
using CourseApp.Api.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddScoped<IProducerService, SqsProducerService>();
builder.Services.AddScoped<ICourseService, CourseService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/courses", async (int id, ICourseService courseService) =>
    Results.Ok(await courseService.GetCourse(id)));

app.Run();
