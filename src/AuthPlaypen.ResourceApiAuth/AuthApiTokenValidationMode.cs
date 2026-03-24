namespace AuthPlaypen.ResourceApiAuth;

public enum AuthApiTokenValidationMode
{
    Jwt = 0,
    [Obsolete("Introspection mode is no longer supported in this package.")]
    Introspection = 1
}
