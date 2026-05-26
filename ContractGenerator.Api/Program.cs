using ContractGenerator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEmployeeGenerator, BogusEmployeeGenerator>();
builder.Services.AddScoped<IEmployeeService, CachedEmployeeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => origin.StartsWith("https://localhost") || origin.StartsWith("http://localhost"))
            .WithHeaders("Content-Type")
            .WithMethods("GET");
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("LocalPolicy");
app.MapControllers();

app.Run();
