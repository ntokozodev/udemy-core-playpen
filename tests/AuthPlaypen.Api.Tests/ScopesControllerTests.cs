using AuthPlaypen.Api.Controllers;
using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthPlaypen.Api.Tests;

public class ScopesControllerTests
{
    private readonly Mock<IScopeService> _scopeService = new();

    [Fact]
    public async Task GetPage_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Arrange
        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.GetPage(cursor: null, pageSize: 101, CancellationToken.None);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Invalid page size");
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Arrange
        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.Search(term: "read", pageSize: 0, CancellationToken.None);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Invalid page size");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _scopeService
            .Setup(service => service.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScopeDto?)null);

        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationErrorIsReturned()
    {
        // Arrange
        var request = new CreateScopeRequest("display", "scope.read", "description", null);
        _scopeService
            .Setup(service => service.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Invalid description"));

        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ProblemDetails>()
            .Which.Status.Should().Be(400);
    }

    [Fact]
    public async Task Update_ShouldReturnConflict_WhenDuplicateErrorIsReturned()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateScopeRequest("display", "scope.read", "description", null);
        _scopeService
            .Setup(service => service.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Scope already exists", false));

        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.Update(id, request, CancellationToken.None);

        // Assert
        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeOfType<ProblemDetails>()
            .Which.Status.Should().Be(409);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenServiceMarksEntityAsMissing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _scopeService
            .Setup(service => service.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, true));

        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
