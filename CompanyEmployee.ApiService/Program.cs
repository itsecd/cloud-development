using CompanyEmployee.ApiService.Services;
using CompanyEmployee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.WithOrigins("https://localhost:7282")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<CompanyEmployeeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseCors("wasm");

app.MapGet("/api/CompanyEmployee", async (HttpContext context, CompanyEmployeeService service, int id) =>
{
    var employee = await service.GetEmployeeAsync(id);

    return Results.Ok(employee);
})
.WithSummary("Получение сотрудника по идентификатору")
.WithDescription("Возвращает информацию о сотруднике по id");

app.Run();