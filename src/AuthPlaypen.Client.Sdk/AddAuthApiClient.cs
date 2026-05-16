using Microsoft.Extensions.DependencyInjection;

namespace AuthPlaypen.Client;

public static class AuthApiClientServiceCollectionExtensions
{
    public static IServiceCollection AddAuthPlaypenClientSdk(this IServiceCollection services, Action<AuthApiClientOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var options = new AuthApiClientOptions();
        configure(options);
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out _)) throw new InvalidOperationException("AuthApiClientOptions.Authority must be an absolute URI.");
        if (string.IsNullOrWhiteSpace(options.ClientId)) throw new InvalidOperationException("AuthApiClientOptions.ClientId is required.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret)) throw new InvalidOperationException("AuthApiClientOptions.ClientSecret is required.");

        services.Configure(configure);
        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client => client.BaseAddress = new Uri(options.Authority, UriKind.Absolute));
        return services;
    }
}
