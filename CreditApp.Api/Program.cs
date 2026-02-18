using CreditApp.Application.Interfaces;
using CreditApp.Application.Services;
using CreditApp.Infrastructure.Generators;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("credit-cache");

var centralBankRate = builder.Configuration.GetValue<double>("CreditGenerator:CentralBankRate", 16.0);

builder.Services.AddSingleton<ICreditApplicationGenerator>(
    new CreditApplicationGenerator(centralBankRate));

builder.Services.AddScoped<ICreditService, CreditService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("DefaultCors");

app.MapControllers();

app.Run();
