using EduApi.Data.Models;
using Moq;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class ElectricitySubscriptionServiceTests
{
    private readonly Mock<IElectricitySubscriptionRepository> _repositoryMock = new();
    private readonly ElectricitySubscriptionService _service;

    public ElectricitySubscriptionServiceTests()
    {
        _service = new ElectricitySubscriptionService(_repositoryMock.Object);
    }

    [Fact]
    public async Task UpsertAsync_ShouldCreateSubscription_WhenSubscriptionDoesNotExist()
    {
        var request = new CreateElectricitySubscriptionRequest
        {
            Url = " https://example.com/wxAccount?id=1 ",
            Email = "User@Example.com ",
            Threshold = 18.5
        };

        _repositoryMock
            .Setup(x => x.GetByEmailAsync("user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElectricitySubscription?)null);

        ElectricitySubscription? savedEntity = null;
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ElectricitySubscription>(), It.IsAny<CancellationToken>()))
            .Callback<ElectricitySubscription, CancellationToken>((subscription, _) => savedEntity = subscription)
            .Returns(Task.CompletedTask);

        var result = await _service.UpsertAsync(request);

        Assert.NotNull(savedEntity);
        Assert.Equal("https://example.com/wxAccount?id=1", savedEntity!.ElectricityUrl);
        Assert.Equal("user@example.com", savedEntity.Email);
        Assert.Equal(18.5, savedEntity.Threshold);
        Assert.True(savedEntity.IsActive);
        Assert.Equal(savedEntity.Id, result.Id);
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdateExistingSubscription_WhenSubscriptionAlreadyExists()
    {
        var existing = new ElectricitySubscription
        {
            Id = "sub-1",
            ElectricityUrl = "https://example.com/wxAccount?id=2",
            Email = "user@example.com",
            Threshold = 10
        };

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(existing.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateElectricitySubscriptionRequest
        {
            Url = existing.ElectricityUrl,
            Email = existing.Email,
            Threshold = 25
        };

        var result = await _service.UpsertAsync(request);

        _repositoryMock.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(25, existing.Threshold);
        Assert.Equal("sub-1", result.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenSubscriptionDoesNotExist()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElectricitySubscription?)null);

        var result = await _service.DeleteAsync("missing");

        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ElectricitySubscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}