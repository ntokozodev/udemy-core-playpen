using System.Net;
using System.Net.Http.Json;
using System.Text;
using AuthPlaypen.ResourceApiAuth;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthPlaypen.ResourceApiAuth.Tests;

public class AuthApiClientTests
{
    [Fact]
    public void AddAuthApiClient_ShouldThrow_WhenClientIdIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAuthApiClient(options =>
        {
            options.Authority = "https://issuer.example";
            options.ClientSecret = "secret";
        });

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("ClientId is required", exception.Message);
    }

    [Fact]
    public async Task RequestClientCredentialsTokenAsync_ShouldReturnToken_WhenAuthApiRespondsSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthApiClient(options =>
        {
            options.Authority = "https://issuer.example";
            options.ClientId = "api-client";
            options.ClientSecret = "api-secret";
        });

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    access_token = "abc123",
                    token_type = "Bearer",
                    expires_in = 3600,
                    scope = "scope.read"
                })
            });

        services.AddSingleton(handler);
        services.AddHttpClient<IAuthApiClient, AuthApiClient>()
            .ConfigurePrimaryHttpMessageHandler(provider => provider.GetRequiredService<StubHttpMessageHandler>());

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IAuthApiClient>();

        // Act
        var token = await client.RequestClientCredentialsTokenAsync(["scope.read"]);

        // Assert
        Assert.Equal("abc123", token.AccessToken);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(3600, token.ExpiresIn);
    }

    [Fact]
    public async Task IntrospectTokenAsync_ShouldReturnIntrospectionPayload_WhenAuthApiRespondsSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthApiClient(options =>
        {
            options.Authority = "https://issuer.example";
            options.ClientId = "api-client";
            options.ClientSecret = "api-secret";
        });

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"active\":true,\"sub\":\"user-1\",\"scope\":\"scope.read\"}", Encoding.UTF8, "application/json")
            });

        services.AddSingleton(handler);
        services.AddHttpClient<IAuthApiClient, AuthApiClient>()
            .ConfigurePrimaryHttpMessageHandler(provider => provider.GetRequiredService<StubHttpMessageHandler>());

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IAuthApiClient>();

        // Act
        var response = await client.IntrospectTokenAsync("token-value");

        // Assert
        Assert.True(response.Active);
        Assert.Equal("user-1", response.Subject);
        Assert.Equal("scope.read", response.Scope);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
