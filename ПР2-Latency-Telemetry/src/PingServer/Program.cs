using System.Net;
using Protocol;
using Transport;

const int Port = 6055;

// Параметры имитации сетевых искажений — без них локальный обмен идеален (RTT≈0, потерь нет)
// и телеметрия (детект потерь/дублей/опозданий) нечем было бы продемонстрировать.
const double LossProbability = 0.05;
const double DuplicateProbability = 0.05;
const int BaseDelayMinMs = 5;
const int BaseDelayMaxMs = 40;
const double DelaySpikeProbability = 0.08;
const int DelaySpikeMinMs = 200;
const int DelaySpikeMaxMs = 400;

var random = new Random();
using var transport = new UdpTransport(Port);
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine($"PING/PONG сервер слушает UDP-порт {Port}. Ctrl+C для остановки.");

while (!cts.IsCancellationRequested)
{
    (byte[] Data, IPEndPoint RemoteEndPoint) received;
    try
    {
        received = await transport.ReceiveAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    PingPongPacket packet;
    try
    {
        packet = PacketCodec.Decode(received.Data);
    }
    catch (MalformedPacketException ex)
    {
        Log($"[{received.RemoteEndPoint}] Отброшен некорректный пакет: {ex.Message}");
        continue;
    }

    if (packet.Type != PacketType.Ping)
    {
        Log($"[{received.RemoteEndPoint}] Ожидался PING, получен {packet.Type} (seq={packet.SequenceNumber}) — игнорируется");
        continue;
    }

    if (random.NextDouble() < LossProbability)
    {
        Log($"[{received.RemoteEndPoint}] PING seq={packet.SequenceNumber} — имитация потери, PONG не отправляется");
        continue;
    }

    _ = RespondAsync(received.RemoteEndPoint, packet);
}

async Task RespondAsync(IPEndPoint remote, PingPongPacket ping)
{
    int delayMs = random.NextDouble() < DelaySpikeProbability
        ? random.Next(DelaySpikeMinMs, DelaySpikeMaxMs + 1)
        : random.Next(BaseDelayMinMs, BaseDelayMaxMs + 1);
    await Task.Delay(delayMs);

    var pong = PacketCodec.Encode(new PingPongPacket(PacketType.Pong, ping.SequenceNumber, ping.ClientTimestampTicks));
    await transport.SendAsync(pong, remote);
    Log($"[{remote}] PING seq={ping.SequenceNumber} -> PONG (задержка ответа {delayMs} мс)");

    if (random.NextDouble() < DuplicateProbability)
    {
        await transport.SendAsync(pong, remote);
        Log($"[{remote}] PING seq={ping.SequenceNumber} — имитация дублирования, PONG отправлен повторно");
    }
}

void Log(string message) =>
    Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} {message}");
