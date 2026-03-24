using AuthPlaypen.Api.Controllers;
using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class ApplicationsControllerTests
{
    private readonly Mock<IApplicationService> _applicationService = new();

    [Fact]
    public async Task GetPage_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Arrange
        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.GetPage(cursor: null, pageSize: 0, CancellationToken.None);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Invalid page size");
    }

    [Fact]
    public async Task GetPage_ShouldReturnBadRequest_WhenCursorIsInvalid()
    {
        // Arrange
        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.GetPage(cursor: "not-a-guid", pageSize: 10, CancellationToken.None);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Invalid cursor");
    }

    [Fact]
    public async Task Search_ShouldReturnEmptyCollection_WhenTermIsWhitespace()
    {
        // Arrange
        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.Search(term: "   ", pageSize: 20, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyCollection<ApplicationReferenceDto>>()
            .Which.Should().BeEmpty();
        _applicationService.Verify(service => service.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenApplicationDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _applicationService
            .Setup(service => service.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationDto?)null);

        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenDuplicateErrorIsReturned()
    {
        // Arrange
        var request = new CreateApplicationRequest("app", "client", "secret", Domain.Entities.ApplicationFlow.ClientCredentials, null, null, []);
        _applicationService
            .Setup(service => service.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "ClientId already exists"));

        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeOfType<ProblemDetails>()
            .Which.Status.Should().Be(409);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenServiceReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateApplicationRequest("app", "client", "secret", Domain.Entities.ApplicationFlow.ClientCredentials, null, null, []);
        _applicationService
            .Setup(service => service.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null, true));

        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.Update(id, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDeleteSucceeds()
    {
        // Arrange
        var id = Guid.NewGuid();
        _applicationService
            .Setup(service => service.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new ApplicationsController(_applicationService.Object);

        // Act
        var result = await controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
