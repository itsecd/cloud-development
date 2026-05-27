namespace ProjectApp.FileService.Function;

public class QueueMessageProcessor(CreditApplicationObjectStorage objectStorage)
{
    public async Task<int> ProcessMessagesAsync(QueueRequest? request)
    {
        var processed = 0;

        foreach (var queueEvent in request?.Messages ?? [])
        {
            var rawBody = queueEvent.Details?.Message?.Body;
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                continue;
            }

            var generatedEvent = QueueMessageReader.TryReadGeneratedEvent(rawBody);
            if (generatedEvent is null)
            {
                Console.WriteLine("[WARN] Failed to deserialize queue message");
                continue;
            }

            await objectStorage.SaveAsync(generatedEvent);
            processed++;
        }

        return processed;
    }
}
