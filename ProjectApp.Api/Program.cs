using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SQS;
using ProjectApp.Api.Messaging;
using ProjectApp.Api.Services.CreditApplicationGeneratorService;
using ProjectApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("cache");

var sqsOptions = new AWSOptions
{
    Credentials = new BasicAWSCredentials(
        builder.Configuration["Aws:AccessKey"] ?? "test",
        builder.Configuration["Aws:SecretKey"] ?? "test"),
    Region = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["Aws:Region"] ?? "us-east-1")
};
sqsOptions.DefaultClientConfig.ServiceURL = builder.Configuration["Sqs:ServiceUrl"] ?? "http://localhost:4566";
sqsOptions.DefaultClientConfig.AuthenticationRegion = builder.Configuration["Aws:Region"] ?? "us-east-1";
builder.Services.AddDefaultAWSOptions(sqsOptions);
builder.Services.AddAWSService<IAmazonSQS>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5127")
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Services.AddSingleton<CreditApplicationGenerator>();
builder.Services.AddScoped<ICreditApplicationGeneratorService, CreditApplicationGeneratorService>();
builder.Services.AddScoped<CreditApplicationGeneratedEventPublisher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Credit Application API"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    var domainXmlPath = Path.Combine(AppContext.BaseDirectory, "ProjectApp.Domain.xml");
    if (File.Exists(domainXmlPath))
    {
        options.IncludeXmlComments(domainXmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
