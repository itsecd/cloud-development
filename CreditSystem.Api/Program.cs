var builder = WebApplication.CreateBuilder(args);

// Подключаем стандартные настройки (ServiceDefaults)
builder.AddServiceDefaults();

// Наши сервисы
builder.Services.AddSingleton<CreditSystem.Api.Services.LoanDataGenerator>();
builder.Services.AddScoped<CreditSystem.Api.Services.LoanService>();

// Кэш (Redis подхватится через Aspire)
builder.AddRedisDistributedCache("cache");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
