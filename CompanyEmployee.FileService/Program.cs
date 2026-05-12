using CompanyEmployee.FileService.Consumers;
using CompanyEmployee.FileService.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<MinioService>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CompanyEmployeeConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
        cfg.Host(new Uri(connectionString!));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var minio = scope.ServiceProvider.GetRequiredService<MinioService>();
        await minio.InitializeAsync();

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

app.Run();