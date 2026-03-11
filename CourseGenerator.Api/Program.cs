using CourseGenerator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ICourseContractGenerator, CourseContractGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/courses/generate", (int count, ICourseContractGenerator generator) =>
    {
        if (count is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["count"] = ["Count must be between 1 and 100."]
            });
        }

        var contracts = generator.Generate(count);
        return Results.Ok(contracts);
    })
    .WithName("GenerateCourses")
    .WithOpenApi();

app.Run();