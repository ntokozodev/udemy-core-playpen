using System.Net;
using System.Text;
using AuthPlaypen.Client;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthPlaypen.Client.Sdk.Tests;

public class AuthApiClientTests
{
    [Fact]
    public async Task RequestClientCredentialsTokenAsync_SendsExpectedPayload_AndParsesToken()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            var json = """
                {
                  "access_token": "token-123",
                  "token_type": "Bearer",
                  "expires_in": 3600
                }
                """;
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.local") };
        var client = BuildClient(httpClient);

        // Act
        var result = await client.RequestClientCredentialsTokenAsync(["orders.read", "orders.write"]);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/connect/token", capturedRequest.RequestUri!.AbsolutePath);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("grant_type=client_credentials", body);
        Assert.Contains("client_id=sdk-client", body);
        Assert.Contains("client_secret=sdk-secret", body);
        Assert.Contains("scope=orders.read+orders.write", body);

        Assert.Equal("token-123", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task RequestClientCredentialsTokenAsync_WithoutScopes_DoesNotSendScopeField()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            var json = """
                {
                  "access_token": "token-123",
                  "token_type": "Bearer",
                  "expires_in": 3600
                }
                """;
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.local") };
        var client = BuildClient(httpClient);

        // Act
        _ = await client.RequestClientCredentialsTokenAsync();

        // Assert
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.DoesNotContain("scope=", body);
    }

    [Fact]
    public async Task IntrospectTokenAsync_ThrowsFriendlyException_WhenApiFails()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("invalid token")
        }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.local") };
        var client = BuildClient(httpClient);

        // Act
        var ex = await Assert.ThrowsAsync<AuthApiClientException>(() => client.IntrospectTokenAsync("abc-token"));

        // Assert
        Assert.Contains("Introspection request failed (400)", ex.Message);
        Assert.Contains("invalid token", ex.Message);
    }

    [Fact]
    public async Task GetPermissionScopeMapAsync_ParsesPermissionsEnvelope_AndDeduplicatesScopes()
    {
        // Arrange
        var json = """
            {
              "permissions": {
                "orders.read": ["orders.read", "orders.read", "orders.list"],
                "orders.write": "orders.write"
              }
            }
            """;

        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.local") };
        var client = BuildClient(httpClient);

        // Act
        var map = await client.GetPermissionScopeMapAsync();

        // Assert
        Assert.Equal(2, map.Count);
        Assert.Equal(["orders.read", "orders.list"], map["orders.read"]);
        Assert.Equal(["orders.write"], map["orders.write"]);
    }

    [Fact]
    public async Task GetJsonWebKeySetAsync_UsesDiscoveryDocumentJwksUri()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/openid-configuration")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"issuer\":\"https://auth.local\",\"jwks_uri\":\"/jwks\"}", Encoding.UTF8, "application/json")
                });
            }

            if (request.RequestUri!.AbsolutePath == "/jwks")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"k1\"}]}", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.local") };
        var client = BuildClient(httpClient);

        // Act
        var jwks = await client.GetJsonWebKeySetAsync();

        // Assert
        Assert.Single(jwks.Keys);
        Assert.Equal("k1", jwks.Keys[0].KeyId);
    }


    [Fact]
    public async Task GetJsonWebKeySetAsync_ThrowsFriendlyException_WhenDiscoveryHasNoJwksUri()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/openid-configuration")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"issuer\":\"https://auth.local\"}", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.local") };
        var client = BuildClient(httpClient);

        // Act
        var ex = await Assert.ThrowsAsync<AuthApiClientException>(() => client.GetJsonWebKeySetAsync());

        // Assert
        Assert.Contains("jwks_uri", ex.Message);
    }

    private static AuthApiClient BuildClient(HttpClient httpClient)
    {
        var options = Options.Create(new AuthApiClientOptions
        {
            ClientId = "sdk-client",
            ClientSecret = "sdk-secret"
        });

        return new AuthApiClient(httpClient, options);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request);
    }
}
