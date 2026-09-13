using Telemetry;
using Xunit;

namespace Tests;

public class LatencyTrackerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void OnTimeResponse_ComputesCorrectRtt()
    {
        var tracker = new LatencyTracker(Timeout);
        var sentAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        tracker.RegisterSent(1, sentAt);

        var outcome = tracker.OnResponseReceived(1, sentAt.AddMilliseconds(30));

        Assert.Equal(ResponseStatus.OnTime, outcome.Status);
        Assert.Equal(30.0, outcome.RttMs!.Value, precision: 3);
    }

    [Fact]
    public void DuplicateResponse_SecondReplyIsFlaggedAndNotDoubleCounted()
    {
        var tracker = new LatencyTracker(Timeout);
        var sentAt = DateTime.UtcNow;
        tracker.RegisterSent(1, sentAt);

        var first = tracker.OnResponseReceived(1, sentAt.AddMilliseconds(10));
        var second = tracker.OnResponseReceived(1, sentAt.AddMilliseconds(15));

        Assert.Equal(ResponseStatus.OnTime, first.Status);
        Assert.Equal(ResponseStatus.Duplicate, second.Status);

        var snapshot = tracker.GetSnapshot();
        Assert.Equal(1, snapshot.TotalOnTime);
        Assert.Equal(1, snapshot.TotalDuplicate);
    }

    [Fact]
    public void UnregisteredSequence_ReturnsUnknown()
    {
        var tracker = new LatencyTracker(Timeout);
        var outcome = tracker.OnResponseReceived(999, DateTime.UtcNow);

        Assert.Equal(ResponseStatus.Unknown, outcome.Status);
        Assert.Equal(1, tracker.GetSnapshot().TotalUnknown);
    }

    [Fact]
    public void Tick_MarksOverdueRequestsAsLost()
    {
        var tracker = new LatencyTracker(Timeout);
        var sentAt = DateTime.UtcNow;
        tracker.RegisterSent(1, sentAt);

        var newlyLost = tracker.Tick(sentAt.Add(Timeout).AddMilliseconds(1));

        Assert.Single(newlyLost);
        Assert.Equal(1u, newlyLost[0]);
        Assert.Equal(1, tracker.GetSnapshot().TotalLost);
    }

    [Fact]
    public void ResponseAfterTimeout_IsLateAndDoesNotReduceLossCount()
    {
        var tracker = new LatencyTracker(Timeout);
        var sentAt = DateTime.UtcNow;
        tracker.RegisterSent(1, sentAt);

        tracker.Tick(sentAt.Add(Timeout).AddMilliseconds(1)); // помечен потерянным
        var lateOutcome = tracker.OnResponseReceived(1, sentAt.Add(Timeout).AddMilliseconds(200));

        Assert.Equal(ResponseStatus.Late, lateOutcome.Status);

        var snapshot = tracker.GetSnapshot();
        Assert.Equal(1, snapshot.TotalLost);
        Assert.Equal(1, snapshot.TotalLate);
        Assert.Equal(1.0, snapshot.LossFraction, precision: 6); // опоздавший не отменяет потерю
    }

    [Fact]
    public void LossFraction_ComputedOverAllSentRequests()
    {
        var tracker = new LatencyTracker(Timeout);
        var sentAt = DateTime.UtcNow;

        for (uint seq = 1; seq <= 4; seq++)
            tracker.RegisterSent(seq, sentAt);

        tracker.OnResponseReceived(1, sentAt.AddMilliseconds(10));
        tracker.OnResponseReceived(2, sentAt.AddMilliseconds(10));
        tracker.Tick(sentAt.Add(Timeout).AddMilliseconds(1)); // 3 и 4 не отвечены -> потеряны

        var snapshot = tracker.GetSnapshot();
        Assert.Equal(4, snapshot.TotalSent);
        Assert.Equal(2, snapshot.TotalOnTime);
        Assert.Equal(2, snapshot.TotalLost);
        Assert.Equal(0.5, snapshot.LossFraction, precision: 6);
    }

    [Fact]
    public void Tick_DoesNotAffectRequestsStillWithinTimeout()
    {
        var tracker = new LatencyTracker(Timeout);
        var sentAt = DateTime.UtcNow;
        tracker.RegisterSent(1, sentAt);

        var newlyLost = tracker.Tick(sentAt.AddMilliseconds(50)); // ещё в пределах таймаута 100мс

        Assert.Empty(newlyLost);
        Assert.Equal(0, tracker.GetSnapshot().TotalLost);
    }
}
