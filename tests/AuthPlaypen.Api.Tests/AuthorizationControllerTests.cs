using AuthPlaypen.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class AuthorizationControllerTests
{
    [Fact]
    public async Task Authorize_ShouldReturnProblem_WhenAzureOidcSchemeIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();

        var schemeProvider = new StubSchemeProvider(Options.Create(new Microsoft.AspNetCore.Authentication.AuthenticationOptions()));
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(schemeProvider);

        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = new OpenIddictRequest() }
        });

        var controller = new AuthorizationController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var result = await controller.Authorize(CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Logout_ShouldReturnSignOutResultWithRootRedirect()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(new StubAuthenticationService());
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var controller = new AuthorizationController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var result = await controller.Logout(CancellationToken.None);

        // Assert
        var signOut = result.Should().BeOfType<SignOutResult>().Subject;
        signOut.Properties?.RedirectUri.Should().Be("/");
        signOut.AuthenticationSchemes.Should().Contain(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
