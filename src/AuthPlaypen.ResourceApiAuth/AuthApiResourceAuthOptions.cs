namespace AuthPlaypen.ResourceApiAuth;

public sealed class AuthApiResourceAuthOptions
{
    public string Authority { get; set; } = AuthApiResourceAuthDefaults.DefaultAuthority;

    public string Audience { get; set; } = string.Empty;

    public AuthApiTokenValidationMode ValidationMode { get; set; } = AuthApiTokenValidationMode.Jwt;

    public bool RequireHttpsMetadata { get; set; } = true;

    [Obsolete("Introspection mode is no longer supported in this package.")]
    public string? IntrospectionClientId { get; set; }

    [Obsolete("Introspection mode is no longer supported in this package.")]
    public string? IntrospectionClientSecret { get; set; }

    [Obsolete("Introspection mode is no longer supported in this package.")]
    public string IntrospectionEndpoint { get; set; } = "/connect/introspect";
}
