using System.Text.Json.Serialization;

namespace AuthPlaypen.Client;

public sealed class AuthApiIntrospectionResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("sub")]
    public string? Subject { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("exp")]
    public long? ExpiresAtUnix { get; set; }
}
