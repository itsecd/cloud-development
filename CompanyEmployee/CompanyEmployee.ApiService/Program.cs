using CompanyEmployee.ApiService.Services;
using CompanyEmployee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Services.AddSingleton<CompanyEmployeeGenerator>();
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

app.MapGet("/api/CompanyEmployee", async (HttpContext context, CompanyEmployeeService service) =>
{
    var idString = context.Request.Query["id"];

    if (!int.TryParse(idString, out var id))
        return Results.BadRequest("Invalid id");

    var employee = await service.GetEmployeeAsync(id);

    return Results.Ok(employee);
});

app.Run();