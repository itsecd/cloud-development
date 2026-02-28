using ProgramProject.GenerationService.Generator;
using ProgramProject.GenerationService.Services;
using ProgramProject.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddRedisDistributedCache("cache");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            try
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost";
            }
            catch
            {
                return false;
            }
        })
        .WithMethods("GET")  // Только GET запросы
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
