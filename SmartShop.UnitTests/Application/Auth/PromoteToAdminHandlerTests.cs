using FluentAssertions;
using Moq;
using SmartShop.Application.Auth.Commands.PromoteToAdmin;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;

namespace SmartShop.UnitTests.Application.Auth;

public class PromoteToAdminHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly PromoteToAdminHandler _handler;

    public PromoteToAdminHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new PromoteToAdminHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_PromotesToAdminAndSaves()
    {
        // Arrange
        var user = User.Create("customer@smartshop.com", "hash", "Test", "User");

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new PromoteToAdminCommand(user.Id), CancellationToken.None);

        // Assert
        user.Role.Should().Be("Admin");
        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistingId))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(
            new PromoteToAdminCommand(nonExistingId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"User with id '{nonExistingId}' was not found.");
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}
