using System.Text.Json;

namespace ProjectApp.FileService.Function;

public class Handler
{
    private readonly QueueMessageProcessor _processor = new(new CreditApplicationObjectStorage());

    public async Task<object> FunctionHandler(QueueRequest request)
    {
        var processed = await _processor.ProcessMessagesAsync(request);
        return new
        {
            statusCode = 200,
            body = JsonSerializer.Serialize(new { processed })
        };
    }
}
