namespace AuthPlaypen.Domain.Entities;

public class ApplicationEntity
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public ApplicationFlow Flow { get; set; }
    public string? PostLogoutRedirectUris { get; set; }
    public string? RedirectUris { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ApplicationScopeEntity> ApplicationScopes { get; set; } = new List<ApplicationScopeEntity>();
}
