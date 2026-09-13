namespace Telemetry;

/// <summary>
/// Джиттер по алгоритму RFC 3550 §6.4.1: J = J + (|D| - J) / 16, где D — разница
/// между соседними измерениями задержки. Здесь D считается по последовательным RTT.
/// </summary>
public sealed class JitterCalculator
{
    private double? _previousRttMs;

    public double JitterMs { get; private set; }

    public double Update(double rttMs)
    {
        if (_previousRttMs is { } previous)
        {
            double d = Math.Abs(rttMs - previous);
            JitterMs += (d - JitterMs) / 16.0;
        }

        _previousRttMs = rttMs;
        return JitterMs;
    }
}
