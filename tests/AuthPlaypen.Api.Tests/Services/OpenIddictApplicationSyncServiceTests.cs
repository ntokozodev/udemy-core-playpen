using AuthPlaypen.Api.Services;
using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Domain.Entities;
using Moq;
using OpenIddict.Abstractions;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class OpenIddictApplicationSyncServiceTests
{
    [Fact]
    public async Task HandleCreationAsync_ShouldConfigurePkceAndRefreshToken_WhenFlowIsAuthorizationWithPkce()
    {
        // Arrange
        var manager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;

        manager
            .Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((descriptor, _) => capturedDescriptor = descriptor)
            .Returns(new ValueTask<object>(new object()));

        var service = new OpenIddictApplicationSyncService(manager.Object);

        var dto = new ApplicationDto(
            Guid.NewGuid(),
            "PKCE App",
            "pkce-app",
            string.Empty,
            ApplicationFlow.AuthorizationWithPKCE,
            "https://localhost:5173/logout-callback",
            "https://localhost:5173/callback",
            [new ScopeReferenceDto(Guid.NewGuid(), "Profile", "profile", "Profile scope")],
            new EntityMetadataDto("test", DateTimeOffset.UtcNow, "test", DateTimeOffset.UtcNow));

        // Act
        await service.HandleCreationAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDescriptor);
        Assert.Equal(OpenIddictConstants.ClientTypes.Public, capturedDescriptor!.ClientType);
        Assert.Contains(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode, capturedDescriptor.Permissions);
        Assert.Contains(OpenIddictConstants.Permissions.GrantTypes.RefreshToken, capturedDescriptor.Permissions);
        Assert.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange, capturedDescriptor.Requirements);
        Assert.Contains(new Uri("https://localhost:5173/callback"), capturedDescriptor.RedirectUris);
    }
}
