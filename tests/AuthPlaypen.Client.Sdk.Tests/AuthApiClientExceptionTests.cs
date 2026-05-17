using AuthPlaypen.Client;
using Xunit;

namespace AuthPlaypen.Client.Sdk.Tests;

public class AuthApiClientExceptionTests
{
    [Fact]
    public void Ctor_SetsMessage()
    {
        var ex = new AuthApiClientException("boom");

        Assert.Equal("boom", ex.Message);
    }
}
