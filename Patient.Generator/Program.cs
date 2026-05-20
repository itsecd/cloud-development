using Patient.Generator.Generator;
using Patient.Generator.Service;
using Patient.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("patient-cache");

builder.Services.AddSingleton<PatientGenerator>();
builder.Services.AddSingleton<IPatientCache, PatientCache>();
builder.Services.AddSingleton<IPatientService, PatientService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
