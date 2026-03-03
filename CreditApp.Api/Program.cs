using CreditApp.Application.Interfaces;
using CreditApp.ServiceDefaults;
using CreditApp.Application.Options;
using CreditApp.Application.Services;
using CreditApp.Infrastructure.Generators;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("credit-cache");

builder.Services.AddSingleton<ICreditApplicationGenerator, CreditApplicationGenerator>();

builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

builder.Services.AddScoped<ICreditService, CreditService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    var domainXmlPath = Path.Combine(AppContext.BaseDirectory, "CreditApp.Domain.xml");
    if (File.Exists(domainXmlPath))
        options.IncludeXmlComments(domainXmlPath);
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins(allowedOrigins);
    });
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

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
