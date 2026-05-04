using Vehicle.EventSink.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddEventSinkServices();

var app = builder.Build();

app.UseEventSinkStartup();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "Vehicle.EventSink",
    status = "ok",
    message = "Vehicle EventSink is running"
}));

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
