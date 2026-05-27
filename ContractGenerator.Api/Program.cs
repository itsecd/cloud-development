using Amazon.SQS;
using ContractGenerator.Api.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();

builder.Services.AddSingleton<IEmployeeGenerator, BogusEmployeeGenerator>();
builder.Services.AddScoped<IEmployeePublisher, SqsEmployeePublisher>();
builder.Services.AddScoped<IEmployeeService, CachedEmployeeService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
