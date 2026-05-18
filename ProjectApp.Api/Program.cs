using ProjectApp.Api.Services.CreditApplicationService;
using ProjectApp.Api.Options;
using ProjectApp.ServiceDefaults;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("cache");
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Services.Configure<CreditApplicationGenerationOptions>(
    builder.Configuration.GetSection(CreditApplicationGenerationOptions.SectionName));
builder.Services.Configure<AwsMessagingOptions>(
    builder.Configuration.GetSection(AwsMessagingOptions.SectionName));

var sqsServiceUrl = builder.Configuration["Services:localstack:HttpEndpoint"] ?? "http://localhost:4566";
builder.Services.AddAWSService<IAmazonSQS>(new AWSOptions
{
    Credentials = new BasicAWSCredentials("test", "test"),
    Region = RegionEndpoint.USEast1,
    DefaultClientConfig =
    {
        ServiceURL = sqsServiceUrl,
        AuthenticationRegion = "us-east-1"
    }
});
builder.Services.AddSingleton<CreditApplicationGenerator>();
builder.Services.AddSingleton<ICreditApplicationEventPublisher, SqsCreditApplicationEventPublisher>();
builder.Services.AddScoped<ICreditApplicationService, CreditApplicationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Credit Application Generator API"
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

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
