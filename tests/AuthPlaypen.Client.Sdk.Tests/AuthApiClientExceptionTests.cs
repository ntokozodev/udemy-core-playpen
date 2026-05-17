using AuthPlaypen.Client.Sdk;

namespace AuthPlaypen.Client.Sdk.Tests;

public class AuthApiClientExceptionTests
{
    [Fact]
    public void Ctor_SetsStatusCode_AndMessage()
    {
        var ex = new AuthApiClientException("boom", 400);

        Assert.Equal("boom", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }
}
