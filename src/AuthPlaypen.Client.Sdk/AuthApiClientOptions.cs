namespace AuthPlaypen.Client;

public sealed class AuthApiClientOptions
{
    public string Authority { get; set; } = "https://localhost:5100";
    public string TokenEndpoint { get; set; } = "/connect/token";
    public string IntrospectionEndpoint { get; set; } = "/connect/introspect";
    public string PermissionMetadataEndpoint { get; set; } = "/.well-known/authplaypen/permissions";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
