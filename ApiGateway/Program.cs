using ApiGateway.Configuration;
using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.Configure<WeightedRoundRobinOptions>(
    builder.Configuration.GetSection(WeightedRoundRobinOptions.SectionName));

builder.Services.AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer<WeightedRoundRobinBalancer>(sp =>
        new WeightedRoundRobinBalancer(
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WeightedRoundRobinOptions>>(),
            sp.GetRequiredService<ILogger<WeightedRoundRobinBalancer>>()));

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(["http://localhost:5127", "https://localhost:7282"]);
    policy.WithMethods("GET");
    policy.WithHeaders("Content-Type");
    policy.WithExposedHeaders("X-Service-Replica", "X-Service-Weight");
}));

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
