using CloudDevelopment.ServiceDefaults;
using Generator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            try
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost";
            }
            catch
            {
                return false;
            }
        })
        .WithMethods("GET")
        .AllowAnyHeader());
});

builder.AddRedisDistributedCache(connectionName: "RedisCache");

builder.Services.AddScoped<CreditOrderGenerator>();
builder.Services.AddScoped<CreditOrderService>();

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("wasm");

app.MapControllers();

app.Run();
