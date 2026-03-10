using CompanyEmployee.ServiceDefaults;
using CompanyEmployee.Generator.Service;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddRedisDistributedCache(connectionName: "cache");

builder.Services.AddSingleton<ICompanyEmployeeGenerator, CompanyEmployeeGenerator>();
builder.Services.AddSingleton<ICompanyEmployeeService, CompanyEmployeeService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();