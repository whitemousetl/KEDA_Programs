using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;
using Moq;

namespace KEDA_Share.Test.Repository.Mongo;

public class MongoDbContextTest
{
    [Fact]
    public async Task InsertAsync_ShouldBeCalled()
    {
        //Arrange
        var mockContext = new Mock<IMongoDbContext<Workstation>>();
        var workstation = new Workstation
        {
            EdgeID = "test",
            EdgeName = "test",
            Timestamp = 1,
            Time = "2024-01-01",
            Protocols = []
        };

        mockContext.Setup(x => x.InsertAsync(It.IsAny<Workstation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        //Act
        await mockContext.Object.InsertAsync(workstation);

        //Assert
        mockContext.Verify(
            x => x.InsertAsync(
                It.Is<Workstation>(w => w.EdgeID == "test" && w.EdgeName == "test"), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task FindLatestByAsync_ShouldReturnExceptedWorkstation()
    {
        // Arrange
        var expected = new Workstation
        {
            EdgeID = "test",
            EdgeName = "test",
            Timestamp = 1,
            Time = "2024-01-01",
            Protocols = []
        };

        var mockContext = new Mock<IMongoDbContext<Workstation>>();
        mockContext.Setup(x => x.FindLatestByAsync(It.IsAny<Func<Workstation, long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await mockContext.Object.FindLatestByAsync(x => x.Timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.EdgeID);
        Assert.Equal("test", result.EdgeName);
    }
}