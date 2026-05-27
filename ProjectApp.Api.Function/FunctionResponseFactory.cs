namespace ProjectApp.Api.Function;

public static class FunctionResponseFactory
{
    public static FunctionResponse Json(int statusCode, string body)
        => new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Methods"] = "GET,OPTIONS",
                ["Access-Control-Allow-Headers"] = "Content-Type,Authorization"
            },
            Body = body
        };
}
