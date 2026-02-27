using ElBruno.Realtime.SileroVad;

namespace ElBruno.Realtime.Tests;

/// <summary>Tests for SileroVadDetector (non-download tests).</summary>
public class SileroVadTests
{
    [Fact]
    public void GetService_ReturnsSelfForOwnType()
    {
        using var detector = new SileroVadDetector();
        var service = detector.GetService(typeof(SileroVadDetector));
        Assert.Same(detector, service);
    }

    [Fact]
    public void GetService_ReturnsNullForUnknownType()
    {
        using var detector = new SileroVadDetector();
        var service = detector.GetService(typeof(string));
        Assert.Null(service);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var detector = new SileroVadDetector();
        detector.Dispose();
        detector.Dispose(); // Should not throw
    }
}
