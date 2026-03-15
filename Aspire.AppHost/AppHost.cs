using System.Text;

var builder = DistributedApplication.CreateBuilder(args);
Console.OutputEncoding = Encoding.UTF32;
Console.InputEncoding = Encoding.UTF32;
var redis = builder.AddRedis("cache");

builder.AddProject<Projects.Asp>("back")
    .WithReference(redis);

builder.AddProject<Projects.Client_Wasm>("front");
builder.Build().Run();
