using Amazon.SimpleNotificationService;
using CompanyEmployee.ApiService.Messaging;
using CompanyEmployee.ApiService.Services;
using CompanyEmployee.DtoModel;
using CompanyEmployee.ServiceDefaults;
using LocalStack.Client.Extensions;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");
builder.Services.AddLocalStack(builder.Configuration);
var awsConfig = builder.Configuration;

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
{
    return new AmazonSimpleNotificationServiceClient(
        awsConfig["AWS:AccessKey"],
        awsConfig["AWS:SecretKey"],
        new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = awsConfig["AWS:ServiceURL"]
        });
});
builder.Services.AddScoped<SnsPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
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

app.MapGet("/api/CompanyEmployee", async (HttpContext context, CompanyEmployeeService service, SnsPublisher endpoint, int id) =>
{
    var employee = await service.GetEmployeeAsync(id);
    var dto = new ModelDTO(
        employee.Id,
        employee.FullName,
        employee.JobTitle,
        employee.Department,
        employee.AdmissionDate,
        employee.Salary,
        employee.Email,
        employee.PhoneNumber,
        employee.Dismissal,
        employee.DismissalDate
    );
    await endpoint.Publish(dto);

    return Results.Ok(employee);
})
.WithSummary("Получение сотрудника по идентификатору")
.WithDescription("Возвращает информацию о сотруднике по id");

app.Run();