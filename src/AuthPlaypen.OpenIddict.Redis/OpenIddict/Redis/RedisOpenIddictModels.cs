using System.Text.Json;

namespace AuthPlaypen.OpenIddict.Redis;

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
    public HashSet<string> Permissions { get; set; } = [];
    public HashSet<string> PostLogoutRedirectUris { get; set; } = [];
    public Dictionary<string, JsonElement> Properties { get; set; } = [];
    public HashSet<string> RedirectUris { get; set; } = [];
    public HashSet<string> Requirements { get; set; } = [];
    public Dictionary<string, string> Settings { get; set; } = [];
}


public sealed class RedisOpenIddictAuthorization
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ApplicationId { get; set; }
    public string? Subject { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? CreationDate { get; set; }
    public HashSet<string> Scopes { get; set; } = [];
    public Dictionary<string, JsonElement> Properties { get; set; } = [];
}

public sealed class RedisOpenIddictScope
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
    public Dictionary<string, string> DisplayNames { get; set; } = [];
    public Dictionary<string, JsonElement> Properties { get; set; } = [];
    public HashSet<string> Resources { get; set; } = [];
}

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
