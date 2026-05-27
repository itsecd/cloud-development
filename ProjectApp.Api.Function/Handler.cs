namespace ProjectApp.Api.Function;

public class Handler
{
    private readonly CreditApplicationGenerator _generator = new();
    private readonly CreditApplicationEventProducer _producer = new();

    public async Task<FunctionResponse> FunctionHandler(FunctionRequest request)
    {
        var id = FunctionRequestIdReader.TryReadId(request);
        if (id is null or <= 0)
        {
            return FunctionResponseFactory.Json(400, """{"error":"id must be a positive integer"}""");
        }

        var application = _generator.Generate(id.Value);
        await _producer.ProduceGeneratedAsync(application);

        return FunctionResponseFactory.Json(200, JsonSerializerDefaultsProvider.Serialize(application));
    }
}
