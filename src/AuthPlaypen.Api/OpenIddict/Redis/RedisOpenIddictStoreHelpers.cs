using System.Text.Json;
using StackExchange.Redis;

namespace AuthPlaypen.Api.OpenIddict.Redis;

internal static class RedisOpenIddictKeys
{
    public static string ApplicationById(string id) => $"oidc:applications:id:{id}";
    public static string ApplicationByClientId(string clientId) => $"oidc:applications:client:{clientId}";

    public static string ScopeById(string id) => $"oidc:scopes:id:{id}";
    public static string ScopeByName(string name) => $"oidc:scopes:name:{name}";

    public static string TokenById(string id) => $"oidc:tokens:id:{id}";
    public static string TokenByReferenceId(string referenceId) => $"oidc:tokens:reference:{referenceId}";
}

internal sealed class RedisOpenIddictSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisValue Serialize<T>(T model) => JsonSerializer.Serialize(model, Options);

    public T? Deserialize<T>(RedisValue value)
    {
        if (value.IsNullOrEmpty)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value!, Options);
    }
}
