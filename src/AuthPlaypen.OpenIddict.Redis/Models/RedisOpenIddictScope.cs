using System.Text.Json;

namespace AuthPlaypen.OpenIddict.Redis.Models;

public sealed class RedisOpenIddictScope
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
    public Dictionary<string, string> DisplayNames { get; set; } = new();
    public Dictionary<string, JsonElement> Properties { get; set; } = new();
    public HashSet<string> Resources { get; set; } = new();
}
