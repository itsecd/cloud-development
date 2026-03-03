namespace CreditApp.Gateway.Extensions;

/// <summary>
/// Расширения для интеграции Aspire service discovery с Ocelot.
/// Динамически разрешает адреса downstream-сервисов из переменных окружения Aspire
/// и переопределяет маршруты Ocelot через in-memory конфигурацию.
/// </summary>
public static class AspireServiceDiscoveryExtensions
{
    /// <summary>
    /// Разрешает адреса downstream-сервисов из Aspire service discovery
    /// и переопределяет конфигурацию Ocelot.
    /// </summary>
    /// <param name="builder">WebApplicationBuilder с загруженной конфигурацией.</param>
    /// <returns>
    /// Маппинг host:port → имя сервиса для привязки весов в балансировщике.
    /// </returns>
    public static Dictionary<string, string> ResolveDownstreamServices(this WebApplicationBuilder builder)
    {
        var generatorNames = builder.Configuration
            .GetSection("GeneratorServices")
            .Get<string[]>() ?? [];

        var addressOverrides = new List<KeyValuePair<string, string?>>();
        var hostPortToServiceName = new Dictionary<string, string>();
        var schemeResolved = false;

        for (var i = 0; i < generatorNames.Length; i++)
        {
            var name = generatorNames[i];

            var url = builder.Configuration[$"services:{name}:http:0"]
                   ?? builder.Configuration[$"services:{name}:https:0"];

            string resolvedHost, resolvedPort;

            if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                resolvedHost = uri.Host;
                resolvedPort = uri.Port.ToString();

                addressOverrides.Add(new($"Routes:0:DownstreamHostAndPorts:{i}:Host", resolvedHost));
                addressOverrides.Add(new($"Routes:0:DownstreamHostAndPorts:{i}:Port", resolvedPort));

                if (!schemeResolved)
                {
                    addressOverrides.Add(new($"Routes:0:DownstreamScheme", uri.Scheme));
                    schemeResolved = true;
                }
            }
            else
            {
                resolvedHost = builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] ?? "localhost";
                resolvedPort = builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] ?? "0";
            }

            hostPortToServiceName[$"{resolvedHost}:{resolvedPort}"] = name;
        }

        if (addressOverrides.Count > 0)
            builder.Configuration.AddInMemoryCollection(addressOverrides);

        return hostPortToServiceName;
    }
}
