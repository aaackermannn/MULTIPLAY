using System.Net;
using PingClient;
using Protocol;
using Telemetry;
using Transport;

const string ServerHost = "127.0.0.1";
const int ServerPort = 6055;
const int SendIntervalMs = 100;
const int TotalPings = 200;
const int TimeoutMs = 150;
const int TickIntervalMs = 50;
const int GracePeriodMs = 1000;

// Запускается через `dotnet run` из src/PingClient — путь ведёт в ПР2-Latency-Telemetry/docs.
const string CsvPath = "../../docs/latency_samples.csv";

var serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerHost), ServerPort);
using var transport = new UdpTransport();
var tracker = new LatencyTracker(TimeSpan.FromMilliseconds(TimeoutMs));
using var csv = new LatencyCsvWriter(CsvPath);
using var cts = new CancellationTokenSource();

Console.WriteLine($"Отправка {TotalPings} PING на {ServerHost}:{ServerPort}, интервал {SendIntervalMs} мс, таймаут {TimeoutMs} мс");

var receiveTask = ReceiveLoopAsync(cts.Token);
var tickTask = TickLoopAsync(cts.Token);

for (uint seq = 1; seq <= TotalPings; seq++)
{
    var sentAt = DateTime.UtcNow;
    var ping = new PingPongPacket(PacketType.Ping, seq, sentAt.Ticks);
    tracker.RegisterSent(seq, sentAt);
    await transport.SendAsync(PacketCodec.Encode(ping), serverEndPoint);
    await Task.Delay(SendIntervalMs);
}

Console.WriteLine($"Все {TotalPings} PING отправлены, ждём {GracePeriodMs} мс на дозапись хвостовых ответов...");
await Task.Delay(GracePeriodMs);
cts.Cancel();
await Task.WhenAll(Swallow(receiveTask), Swallow(tickTask));

var snapshot = tracker.GetSnapshot();
Console.WriteLine("=== Итоговая статистика ===");
Console.WriteLine($"Отправлено: {snapshot.TotalSent}, вовремя: {snapshot.TotalOnTime}, потеряно: {snapshot.TotalLost}, " +
                   $"опоздавших: {snapshot.TotalLate}, дублей: {snapshot.TotalDuplicate}, неизвестных: {snapshot.TotalUnknown}");
Console.WriteLine($"Доля потерь: {snapshot.LossFraction:P2}");
Console.WriteLine($"SRTT: {snapshot.SmoothedRttMs:F2} мс, джиттер: {snapshot.JitterMs:F2} мс");
Console.WriteLine($"Журнал измерений записан: {Path.GetFullPath(CsvPath)}");

async Task ReceiveLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        (byte[] Data, IPEndPoint RemoteEndPoint) received;
        try
        {
            received = await transport.ReceiveAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        PingPongPacket packet;
        try
        {
            packet = PacketCodec.Decode(received.Data);
        }
        catch (MalformedPacketException ex)
        {
            Console.WriteLine($"Отброшен некорректный пакет от {received.RemoteEndPoint}: {ex.Message}");
            continue;
        }

        if (packet.Type != PacketType.Pong)
            continue;

        var now = DateTime.UtcNow;
        var outcome = tracker.OnResponseReceived(packet.SequenceNumber, now);
        var snapshotNow = tracker.GetSnapshot();
        csv.WriteRow(packet.SequenceNumber, now, outcome.Status, outcome.RttMs, outcome.SmoothedRttMs, outcome.JitterMs, snapshotNow.LossFraction);

        switch (outcome.Status)
        {
            case ResponseStatus.OnTime:
                Console.WriteLine($"[seq={packet.SequenceNumber}] OnTime RTT={outcome.RttMs:F2} мс SRTT={outcome.SmoothedRttMs:F2} мс jitter={outcome.JitterMs:F2} мс");
                break;
            case ResponseStatus.Late:
                Console.WriteLine($"[seq={packet.SequenceNumber}] Late (пришёл после таймаута, уже учтён как потеря)");
                break;
            case ResponseStatus.Duplicate:
                Console.WriteLine($"[seq={packet.SequenceNumber}] Duplicate (повторный PONG проигнорирован)");
                break;
            case ResponseStatus.Unknown:
                Console.WriteLine($"[seq={packet.SequenceNumber}] Unknown (ответ на номер, который не отправлялся)");
                break;
        }
    }
}

async Task TickLoopAsync(CancellationToken ct)
{
    try
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TickIntervalMs, ct);
            var newlyLost = tracker.Tick(DateTime.UtcNow);
            if (newlyLost.Count > 0)
            {
                var snapshotNow = tracker.GetSnapshot();
                foreach (var seq in newlyLost)
                {
                    csv.WriteRow(seq, DateTime.UtcNow, ResponseStatus.Lost, null, null, null, snapshotNow.LossFraction);
                    Console.WriteLine($"[seq={seq}] Lost (таймаут {TimeoutMs} мс истёк)");
                }
            }
        }
    }
    catch (OperationCanceledException)
    {
        // штатное завершение по отмене токена
    }
}

static async Task Swallow(Task task)
{
    try { await task; } catch (OperationCanceledException) { }
}
