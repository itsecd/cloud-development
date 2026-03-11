using CourseGenerator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ICourseContractGenerator, CourseContractGenerator>();
builder.Services.AddSingleton<ICourseContractCacheService, CourseContractCacheService>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
    options.InstanceName = "course-generator:";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/courses/generate", async (int count, ICourseContractGenerator generator, ICourseContractCacheService cache, CancellationToken cancellationToken) =>
    {
        if (count is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["count"] = ["Count must be between 1 and 100."]
            });
        }

        var cachedContracts = await cache.GetAsync(count, cancellationToken);
        if (cachedContracts is not null)
        {
            return Results.Ok(cachedContracts);
        }

        var contracts = generator.Generate(count);
        await cache.SetAsync(count, contracts, cancellationToken);

        return Results.Ok(contracts);
    })
    .WithName("GenerateCourses")
    .WithOpenApi();

app.Run();