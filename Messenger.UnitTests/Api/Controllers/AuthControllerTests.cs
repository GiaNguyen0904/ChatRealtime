using FluentAssertions;
using Messenger.Api.Controllers;
using Messenger.Api.Controllers.Auth;
using Messenger.Application.Auth.DTOs;
using Messenger.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Messenger.UnitTests.Api.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_ShouldReturn200Ok_WhenSuccess()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "test@gmail.com", Password = "Password123!" };
        var expectedResponse = new AuthResponseDto { AccessToken = "valid_token" };

        _authServiceMock.Setup(x => x.LoginAsync(request)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }
}