using Amazon.SimpleNotificationService;
using CompanyEmployee.Generator.Messaging;
using CompanyEmployee.ServiceDefaults;
using CompanyEmployee.Generator.Service;
using LocalStack.Client.Extensions;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddRedisDistributedCache(connectionName: "cache");

builder.Services.AddSingleton<ICompanyEmployeeGenerator, CompanyEmployeeGenerator>();
builder.Services.AddSingleton<ICompanyEmployeeService, CompanyEmployeeService>();
builder.Services.AddSingleton<IProducerService, SnsProducerService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();