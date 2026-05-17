using AuthPlaypen.Client;
using Xunit;

namespace AuthPlaypen.Client.Sdk.Tests;

public class AuthApiClientOptionsTests
{
    [Fact]
    public void Default_Values_AreExpected()
    {
        // Arrange
        // (no external dependencies)

        // Act
        var options = new AuthApiClientOptions();

        // Assert
        Assert.Equal("https://localhost:5100", options.Authority);
        Assert.Equal("/connect/token", options.TokenEndpoint);
        Assert.Equal("/connect/introspect", options.IntrospectionEndpoint);
    }
}
