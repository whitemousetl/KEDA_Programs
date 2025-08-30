using FluentAssertions;
using KEDA_Share.Entity;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using Moq;

namespace KEDA_Share.Test.Repository.Implementations;

public class WorkstationRepositoryTest
{
    [Fact]
    public async Task AddAsync_ShouldInsertWorkstation()
    {
        // Arrange
        var mockContext = new Mock<IMongoDbContext<Workstation>>();
        mockContext.Setup(x => x.InsertAsync(It.IsAny<Workstation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repo = new WorkstationRepository(mockContext.Object);

        var workstation = new Workstation
        {
            EdgeID = "edge1",
            EdgeName = "Edge One",
            Timestamp = 100,
            Time = "2024--01-01",
            Protocols = []
        };

        // Act
        await repo.AddAsync(workstation);

        // Assert
        mockContext.Verify(
            x => x.InsertAsync(
                It.Is<Workstation>(w => w.EdgeID == "edge1" && w.EdgeName == "Edge One"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLatestByTimestampAsync_ShouldReturnLatestWorkstation()
    {
        var mockContext = new Mock<IMongoDbContext<Workstation>>();
        mockContext.Setup(x => x
            .FindLatestByAsync(It.IsAny<Func<Workstation, long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Workstation { EdgeID = "e2", Timestamp = 2 });

        var repo = new WorkstationRepository(mockContext.Object);

        var latest = await repo.GetLatestByTimestampAsync();

        latest.Should().NotBeNull();
        latest.EdgeID.Should().Be("e2");
    }
}