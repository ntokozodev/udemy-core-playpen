namespace AuthPlaypen.ResourceApi;

public sealed class PermissionAliasAuthorizationOptions
{
    public string Authority { get; set; } = AuthApiResourceAuthDefaults.DefaultAuthority;

    public string MetadataEndpoint { get; set; } = "/.well-known/authplaypen/permissions";

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan FetchTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan FailureRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    public IDictionary<string, string[]> HardcodedFallbackMappings { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
}
