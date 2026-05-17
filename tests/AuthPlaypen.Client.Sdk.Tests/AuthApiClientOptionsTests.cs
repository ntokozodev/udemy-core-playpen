using AuthPlaypen.Client.Sdk;

namespace AuthPlaypen.Client.Sdk.Tests;

public class AuthApiClientOptionsTests
{
    [Fact]
    public void Default_Values_AreExpected()
    {
        var options = new AuthApiClientOptions();

        Assert.Equal("connect/token", options.TokenPath);
        Assert.Equal("connect/introspect", options.IntrospectionPath);
    }
}
