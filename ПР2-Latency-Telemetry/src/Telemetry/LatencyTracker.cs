namespace Telemetry;

/// <summary>
/// Считает RTT/SRTT/джиттер/долю потерь по фактам отправки PING и получения PONG.
/// Не знает ничего о сокетах или формате пакетов — только временные метки и номера
/// последовательности, переданные вызывающим кодом (Transport + Protocol).
///
/// Семантика потерь: запрос считается потерянным, если ответ не пришёл до вызова
/// <see cref="Tick"/> спустя <c>timeout</c> после отправки. Более поздний ответ на уже
/// потерянный запрос помечается как Late и не уменьшает долю потерь — операционно он
/// опоздал независимо от того, что технически пакет не был потерян сетью.
/// </summary>
public sealed class LatencyTracker
{
    private readonly TimeSpan _timeout;
    private readonly Dictionary<uint, DateTime> _pending = new();
    private readonly HashSet<uint> _acked = new();
    private readonly HashSet<uint> _lost = new();
    private readonly SmoothedRttCalculator _srtt = new();
    private readonly JitterCalculator _jitter = new();
    private readonly object _lock = new();

    private int _totalSent;
    private int _totalLate;
    private int _totalDuplicate;
    private int _totalUnknown;

    public LatencyTracker(TimeSpan timeout) => _timeout = timeout;

    public void RegisterSent(uint sequenceNumber, DateTime sendTimestampUtc)
    {
        lock (_lock)
        {
            _pending[sequenceNumber] = sendTimestampUtc;
            _totalSent++;
        }
    }

    public ResponseOutcome OnResponseReceived(uint sequenceNumber, DateTime receiveTimestampUtc)
    {
        lock (_lock)
        {
            if (_pending.Remove(sequenceNumber, out var sentAt))
            {
                _acked.Add(sequenceNumber);
                double rttMs = (receiveTimestampUtc - sentAt).TotalMilliseconds;
                double srtt = _srtt.Update(rttMs);
                double jitter = _jitter.Update(rttMs);
                return new ResponseOutcome(sequenceNumber, ResponseStatus.OnTime, rttMs, srtt, jitter);
            }

            if (_lost.Contains(sequenceNumber))
            {
                _totalLate++;
                return new ResponseOutcome(sequenceNumber, ResponseStatus.Late);
            }

            if (_acked.Contains(sequenceNumber))
            {
                _totalDuplicate++;
                return new ResponseOutcome(sequenceNumber, ResponseStatus.Duplicate);
            }

            _totalUnknown++;
            return new ResponseOutcome(sequenceNumber, ResponseStatus.Unknown);
        }
    }

    /// <summary>Проверяет отложенные запросы и помечает потерянными те, что превысили таймаут.</summary>
    public IReadOnlyList<uint> Tick(DateTime nowUtc)
    {
        lock (_lock)
        {
            var newlyLost = new List<uint>();
            foreach (var (sequence, sentAt) in _pending)
            {
                if (nowUtc - sentAt > _timeout)
                    newlyLost.Add(sequence);
            }

            foreach (var sequence in newlyLost)
            {
                _pending.Remove(sequence);
                _lost.Add(sequence);
            }

            return newlyLost;
        }
    }

    public LatencyStatsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            double lossFraction = _totalSent == 0 ? 0.0 : (double)_lost.Count / _totalSent;
            return new LatencyStatsSnapshot(
                TotalSent: _totalSent,
                TotalOnTime: _acked.Count,
                TotalLost: _lost.Count,
                TotalLate: _totalLate,
                TotalDuplicate: _totalDuplicate,
                TotalUnknown: _totalUnknown,
                LossFraction: lossFraction,
                SmoothedRttMs: _srtt.SmoothedRttMs,
                JitterMs: _jitter.JitterMs);
        }
    }
}
