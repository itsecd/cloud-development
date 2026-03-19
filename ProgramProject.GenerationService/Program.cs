using ProgramProject.GenerationService.Generator;
using ProgramProject.GenerationService.Services;
using ProgramProject.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Починка кодировки в консоли
Console.OutputEncoding = System.Text.Encoding.UTF8;

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("cache");

builder.Services.AddSingleton<IProgramProjectFaker, ProgramProjectFaker>();
builder.Services.AddScoped<IProjectService, ProjectService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors("AllowClient");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();