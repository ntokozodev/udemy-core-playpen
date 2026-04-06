using System.Text.Json.Serialization;

namespace AuthPlaypen.ResourceApi;

public sealed class AuthApiIntrospectionResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("sub")]
    public string? Subject { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("exp")]
    public long? Exp { get; init; }
}
