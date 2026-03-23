using System.Text.Json;

namespace AuthPlaypen.OpenIddict.Redis.Models;

public sealed class RedisOpenIddictAuthorization
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ApplicationId { get; set; }
    public string? Subject { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? CreationDate { get; set; }
    public HashSet<string> Scopes { get; set; } = new();
    public Dictionary<string, JsonElement> Properties { get; set; } = new();
}
