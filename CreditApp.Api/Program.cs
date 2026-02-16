using CreditApp.Api.Services.CreditGeneratorService;
using CreditApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRedisClient("cache");
builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("cache");
    options.Configuration = connectionString;
});
builder.Services.AddScoped<ICreditApplicationGeneratorService, CreditApplicationGeneratorService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();