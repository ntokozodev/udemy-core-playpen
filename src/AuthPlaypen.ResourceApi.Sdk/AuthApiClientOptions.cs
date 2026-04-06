namespace AuthPlaypen.ResourceApi;

public sealed class AuthApiClientOptions
{
    public string Authority { get; set; } = AuthApiResourceAuthDefaults.DefaultAuthority;

    public string TokenEndpoint { get; set; } = "/connect/token";

    public string IntrospectionEndpoint { get; set; } = "/connect/introspect";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
