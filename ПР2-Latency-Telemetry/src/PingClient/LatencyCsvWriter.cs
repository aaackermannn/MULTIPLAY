using System.Globalization;
using Telemetry;

namespace PingClient;

/// <summary>Журнал измерений в формате docs/latency_samples.csv (требование задания).</summary>
public sealed class LatencyCsvWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public LatencyCsvWriter(string path)
    {
        _writer = new StreamWriter(path, append: false) { AutoFlush = true };
        _writer.WriteLine("SequenceNumber,EventAtUtc,Status,RttMs,SmoothedRttMs,JitterMs,LossFractionSoFar");
    }

    public void WriteRow(uint sequenceNumber, DateTime eventAtUtc, ResponseStatus status,
        double? rttMs, double? smoothedRttMs, double? jitterMs, double lossFractionSoFar)
    {
        var culture = CultureInfo.InvariantCulture;
        string rtt = rttMs?.ToString("F3", culture) ?? "";
        string srtt = smoothedRttMs?.ToString("F3", culture) ?? "";
        string jitter = jitterMs?.ToString("F3", culture) ?? "";

        lock (_lock)
        {
            _writer.WriteLine($"{sequenceNumber},{eventAtUtc:o},{status},{rtt},{srtt},{jitter},{lossFractionSoFar.ToString("F4", culture)}");
        }
    }

    public void Dispose() => _writer.Dispose();
}
