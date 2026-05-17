using System.Text.Json.Serialization;

namespace AuthPlaypen.Client;

public sealed class JsonWebKeySetDocument
{
    [JsonPropertyName("keys")]
    public IReadOnlyList<JsonWebKeyDocument> Keys { get; set; } = Array.Empty<JsonWebKeyDocument>();
}

public sealed class JsonWebKeyDocument
{
    [JsonPropertyName("kty")]
    public string? KeyType { get; set; }

    [JsonPropertyName("use")]
    public string? Use { get; set; }

    [JsonPropertyName("alg")]
    public string? Algorithm { get; set; }

    [JsonPropertyName("kid")]
    public string? KeyId { get; set; }

    [JsonPropertyName("n")]
    public string? Modulus { get; set; }

    [JsonPropertyName("e")]
    public string? Exponent { get; set; }

    [JsonPropertyName("x5t")]
    public string? X5t { get; set; }

    [JsonPropertyName("x5t#S256")]
    public string? X5tS256 { get; set; }

    [JsonPropertyName("x5c")]
    public IReadOnlyList<string> X5c { get; set; } = Array.Empty<string>();
}
