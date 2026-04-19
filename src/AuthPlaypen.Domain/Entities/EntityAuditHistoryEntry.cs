namespace AuthPlaypen.Domain.Entities;

public class EntityAuditHistoryEntry
{
    public Guid Id { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public required string ActorDisplayName { get; set; }
    public string? ActorEmail { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? ChangeSummaryJson { get; set; }
}
