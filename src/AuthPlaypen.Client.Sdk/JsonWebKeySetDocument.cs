using System.Text.Json.Serialization;

namespace AuthPlaypen.Client;

public sealed class JsonWebKeySetDocument
{
    [JsonPropertyName("keys")]
    public IReadOnlyList<JsonWebKeyDocument> Keys { get; init; } = Array.Empty<JsonWebKeyDocument>();
}

public sealed class JsonWebKeyDocument
{
    [JsonPropertyName("kty")]
    public string? KeyType { get; init; }

    [JsonPropertyName("use")]
    public string? Use { get; init; }

    [JsonPropertyName("alg")]
    public string? Algorithm { get; init; }

    [JsonPropertyName("kid")]
    public string? KeyId { get; init; }

    [JsonPropertyName("n")]
    public string? Modulus { get; init; }

    [JsonPropertyName("e")]
    public string? Exponent { get; init; }

    [JsonPropertyName("x5t")]
    public string? X5t { get; init; }

    [JsonPropertyName("x5t#S256")]
    public string? X5tS256 { get; init; }

    [JsonPropertyName("x5c")]
    public IReadOnlyList<string> X5c { get; init; } = Array.Empty<string>();
}
