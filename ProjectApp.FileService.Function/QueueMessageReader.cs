using System.Text;

namespace ProjectApp.FileService.Function;

public static class QueueMessageReader
{
    public static CreditApplicationGeneratedEvent? TryReadGeneratedEvent(string rawBody)
    {
        foreach (var candidate in GetBodyCandidates(rawBody))
        {
            try
            {
                var evt = JsonSerializerDefaultsProvider.Deserialize<CreditApplicationGeneratedEvent>(candidate);
                if (evt?.Application is not null)
                {
                    return evt;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetBodyCandidates(string rawBody)
    {
        var candidates = new List<string> { rawBody };

        try
        {
            candidates.Add(Encoding.UTF8.GetString(Convert.FromBase64String(rawBody)));
        }
        catch
        {
        }

        return candidates;
    }
}
