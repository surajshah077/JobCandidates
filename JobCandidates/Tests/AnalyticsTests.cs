using Xunit;

public class AnalyticsTests
{
    [Fact]
    public void CanComputeSimpleConversionRate()
    {
        int applied = 10;
        int hired = 2;

        var rate = applied == 0 ? 0 : (double)hired / applied * 100;

        Assert.Equal(20, rate);
    }
}