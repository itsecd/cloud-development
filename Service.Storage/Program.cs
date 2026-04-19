using Service.Storage;
using CloudDevelopment.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.AddConsumer();
builder.AddS3();

var app = builder.Build();
await app.UseS3();
app.MapControllers();
app.Run();