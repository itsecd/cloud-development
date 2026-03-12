using CompanyEmployee.Api.Services;
using CompanyEmployee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");
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