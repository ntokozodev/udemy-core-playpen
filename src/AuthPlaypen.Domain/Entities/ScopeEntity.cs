namespace AuthPlaypen.Domain.Entities;

public class ScopeEntity
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public required string ScopeName { get; set; }
    public required string Description { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsGlobal => ApplicationScopes.Count == 0;

    public ICollection<ApplicationScopeEntity> ApplicationScopes { get; set; } = new List<ApplicationScopeEntity>();
}
