using System.Text.Json;

namespace ProjectApp.Api.Function;

public static class JsonSerializerDefaultsProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions);
}
