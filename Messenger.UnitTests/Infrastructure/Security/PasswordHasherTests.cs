using FluentAssertions;
using Messenger.Infrastructure.Security;
using Xunit;

namespace Messenger.UnitTests.Infrastructure.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnHashedPassword()
    {
        // Act
        var hash = _sut.Hash("Password123!");

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("Password123!");
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordIsCorrect()
    {
        // Arrange
        var password = "Password123!";
        var hash = _sut.Hash(password);

        // Act
        var result = _sut.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }
}