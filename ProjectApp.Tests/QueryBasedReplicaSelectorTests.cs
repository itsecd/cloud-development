using ProjectApp.Gateway.LoadBalancing;

namespace ProjectApp.Tests;

public class QueryBasedReplicaSelectorTests
{
    [Theory]
    [InlineData(32, 3, 2)]
    [InlineData(654, 3, 0)]
    [InlineData(55, 3, 1)]
    [InlineData(-1, 3, 2)]
    [InlineData(0, 3, 0)]
    public void SelectIndex_ShouldReturnExpectedReplicaIndex(int requestId, int replicasCount, int expectedIndex)
    {
        var index = QueryBasedReplicaSelector.SelectIndex(requestId, replicasCount);
        Assert.Equal(expectedIndex, index);
    }

    [Fact]
    public void SelectIndex_ShouldThrow_WhenReplicasCountIsInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QueryBasedReplicaSelector.SelectIndex(1, 0));
    }
}
