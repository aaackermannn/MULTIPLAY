namespace Telemetry;

public readonly record struct LatencyStatsSnapshot(
    int TotalSent,
    int TotalOnTime,
    int TotalLost,
    int TotalLate,
    int TotalDuplicate,
    int TotalUnknown,
    double LossFraction,
    double SmoothedRttMs,
    double JitterMs);
