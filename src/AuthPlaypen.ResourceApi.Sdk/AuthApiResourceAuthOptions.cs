namespace AuthPlaypen.ResourceApi;

public sealed class AuthApiResourceAuthOptions
{
    public string Authority { get; set; } = AuthApiResourceAuthDefaults.DefaultAuthority;

    public string Audience { get; set; } = string.Empty;

    public AuthApiTokenValidationMode ValidationMode { get; set; } = AuthApiTokenValidationMode.Jwt;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string? IntrospectionClientId { get; set; }

    public string? IntrospectionClientSecret { get; set; }

    public string IntrospectionEndpoint { get; set; } = "/connect/introspect";
}
