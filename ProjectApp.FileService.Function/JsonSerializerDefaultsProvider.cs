using System.Text.Json;

namespace ProjectApp.FileService.Function;

public static class JsonSerializerDefaultsProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions IndentedJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static T? Deserialize<T>(string value)
        => JsonSerializer.Deserialize<T>(value, JsonOptions);

    public static string SerializeIndented<T>(T value)
        => JsonSerializer.Serialize(value, IndentedJsonOptions);
}
