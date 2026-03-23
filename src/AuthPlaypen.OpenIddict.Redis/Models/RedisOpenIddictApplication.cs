using System.Text.Json;

namespace AuthPlaypen.OpenIddict.Redis.Models;

public sealed class RedisOpenIddictApplication
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? DisplayName { get; set; }
    public string? ApplicationType { get; set; }
    public string? ClientType { get; set; }
    public string? ConsentType { get; set; }
    public string? JsonWebKeySet { get; set; }
    public HashSet<string> Permissions { get; set; } = new();
    public HashSet<string> PostLogoutRedirectUris { get; set; } = new();
    public Dictionary<string, JsonElement> Properties { get; set; } = new();
    public HashSet<string> RedirectUris { get; set; } = new();
    public HashSet<string> Requirements { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
}
