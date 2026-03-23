using Amazon.SimpleNotificationService;
using Amazon.SQS;
using CreditApp.Application.Interfaces;
using CreditApp.ServiceDefaults;
using CreditApp.Application.Options;
using CreditApp.Application.Services;
using CreditApp.Infrastructure.Generators;
using MassTransit;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("credit-cache");

builder.Services.AddSingleton<ICreditApplicationGenerator, CreditApplicationGenerator>();

builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

builder.Services.AddScoped<ICreditService, CreditService>();

var awsServiceUrl = builder.Configuration["Aws:ServiceUrl"] ?? "http://localhost:4566";
var awsRegion = builder.Configuration["Aws:Region"] ?? "us-east-1";
var awsAccessKey = builder.Configuration["Aws:AccessKey"] ?? "test";
var awsSecretKey = builder.Configuration["Aws:SecretKey"] ?? "test";

builder.Services.AddMassTransit(x =>
{
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(awsRegion, h =>
        {
            h.Config(new AmazonSQSConfig { ServiceURL = awsServiceUrl });
            h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = awsServiceUrl });
            h.AccessKey(awsAccessKey);
            h.SecretKey(awsSecretKey);
        });

        cfg.ConfigureEndpoints(context);
    });
});

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
