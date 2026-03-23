namespace AuthPlaypen.OpenIddict.Redis.Models;

public sealed class RedisOpenIddictToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ApplicationId { get; set; }
    public string? AuthorizationId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Subject { get; set; }
    public string? ReferenceId { get; set; }
    public string? Payload { get; set; }
    public string? Properties { get; set; }
    public string? RedemptionDate { get; set; }
    public string? CreationDate { get; set; }
    public string? ExpirationDate { get; set; }
}
