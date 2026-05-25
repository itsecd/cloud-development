using ProjectApp.Domain.Entities;

namespace ProjectApp.Domain.Events;

public class CreditApplicationGeneratedEvent
{
    public required int Id { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required CreditApplication Application { get; init; }
}
