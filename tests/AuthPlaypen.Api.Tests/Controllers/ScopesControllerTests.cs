using AuthPlaypen.Api.Controllers;
using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

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
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Invalid page size", details.Title);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Arrange
        var controller = new ScopesController(_scopeService.Object);

        // Act
        var result = await controller.Search(term: "read", pageSize: 0, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Invalid page size", details.Title);
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
        Assert.IsType<NotFoundResult>(result.Result);
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
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, details.Status);
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
        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, details.Status);
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
        Assert.IsType<NotFoundResult>(result);
    }
}
