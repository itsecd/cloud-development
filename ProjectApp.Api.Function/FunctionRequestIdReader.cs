namespace ProjectApp.Api.Function;

public static class FunctionRequestIdReader
{
    public static int? TryReadId(FunctionRequest? request)
    {
        try
        {
            var rawId = TryGetValue(request?.QueryStringParameters, "id")
                ?? TryGetValue(request?.PathParameters, "id")
                ?? TryGetValue(request?.PathParams, "id");

            if (int.TryParse(rawId, out var id))
            {
                return id;
            }

            var path = request?.Path ?? request?.Url ?? string.Empty;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return ReadIdFromQuery(uri.Query);
            }

            var queryStart = path.IndexOf('?');
            return queryStart >= 0 ? ReadIdFromQuery(path[queryStart..]) : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? ReadIdFromQuery(string query)
    {
        var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 &&
                parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(Uri.UnescapeDataString(parts[1]), out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static string? TryGetValue(Dictionary<string, string>? values, string key)
        => values is not null && values.TryGetValue(key, out var value) ? value : null;
}
