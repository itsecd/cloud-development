using Amazon.SimpleNotificationService;
using CompanyEmployee.Api.Services;
using CompanyEmployee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.Configure<SnsSettings>(builder.Configuration.GetSection("SNS"));

var snsConfig = new AmazonSimpleNotificationServiceConfig
{
    ServiceURL = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566",
    AuthenticationRegion = builder.Configuration["AWS:Region"] ?? "us-east-1",
    UseHttp = true
};

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
    new AmazonSimpleNotificationServiceClient(
        builder.Configuration["AWS:AccessKeyId"] ?? "test",
        builder.Configuration["AWS:SecretAccessKey"] ?? "test",
        snsConfig));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEmployeeGenerator, EmployeeGenerator>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();