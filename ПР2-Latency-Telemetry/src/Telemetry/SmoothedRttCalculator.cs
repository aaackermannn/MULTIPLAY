namespace Telemetry;

/// <summary>
/// Сглаженный RTT по формуле RFC 6298 (тот же алгоритм, что использует TCP для расчёта
/// таймаута ретрансмиссии): SRTT = (1-alpha)*SRTT + alpha*R, RTTVAR = (1-beta)*RTTVAR + beta*|SRTT-R|.
/// </summary>
public sealed class SmoothedRttCalculator
{
    private const double Alpha = 1.0 / 8.0;
    private const double Beta = 1.0 / 4.0;

    private bool _hasSample;

    public double SmoothedRttMs { get; private set; }
    public double RttVariationMs { get; private set; }

    public double Update(double rttMs)
    {
        if (!_hasSample)
        {
            SmoothedRttMs = rttMs;
            RttVariationMs = rttMs / 2.0;
            _hasSample = true;
        }
        else
        {
            RttVariationMs = (1 - Beta) * RttVariationMs + Beta * Math.Abs(SmoothedRttMs - rttMs);
            SmoothedRttMs = (1 - Alpha) * SmoothedRttMs + Alpha * rttMs;
        }

        return SmoothedRttMs;
    }
}
