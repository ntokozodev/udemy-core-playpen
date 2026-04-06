using Microsoft.Extensions.DependencyInjection;

namespace AuthPlaypen.ResourceApi;

public static class AuthApiClientServiceCollectionExtensions
{
    public static IServiceCollection AddAuthApiClient(
        this IServiceCollection services,
        Action<AuthApiClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AuthApiClientOptions();
        configure(options);
        ValidateOptions(options);

        services.Configure(configure);

        services
            .AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
            {
                client.BaseAddress = new Uri(options.Authority, UriKind.Absolute);
            });

        return services;
    }

    private static void ValidateOptions(AuthApiClientOptions options)
    {
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AuthApiClientOptions.Authority must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new InvalidOperationException("AuthApiClientOptions.ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException("AuthApiClientOptions.ClientSecret is required.");
        }
    }
}
