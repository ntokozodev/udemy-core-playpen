using System.Text.Json.Serialization;

namespace AuthPlaypen.Client;

public sealed class OpenIdConfigurationDocument
{
    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("jwks_uri")]
    public string? JwksUri { get; set; }
}
