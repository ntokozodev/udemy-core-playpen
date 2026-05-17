using AuthPlaypen.Client;
using Xunit;

namespace AuthPlaypen.Client.Sdk.Tests;

public class AuthApiClientExceptionTests
{
    [Fact]
    public void Ctor_SetsMessage()
    {
        // Arrange
        // (no arrangement required beyond constructor input)

        // Act
        var ex = new AuthApiClientException("boom");

        // Assert
        Assert.Equal("boom", ex.Message);
    }
}
