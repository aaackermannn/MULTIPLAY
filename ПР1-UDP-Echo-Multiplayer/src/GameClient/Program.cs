using System.Net.Sockets;
using GameProtocol;

const string ServerHost = "127.0.0.1";
const int ServerPort = 5055;
const int SendIntervalMs = 1000;

using var udpClient = new UdpClient();
udpClient.Connect(ServerHost, ServerPort);

var random = new Random();
float x = 0f, y = 0f;
uint sequenceNumber = 0;

Console.WriteLine($"Клиент отправляет команды на {ServerHost}:{ServerPort} раз в {SendIntervalMs} мс. Ctrl+C для остановки.");

while (true)
{
    sequenceNumber++;
    byte[] outgoing;
    string description;

    if (sequenceNumber % 4 == 0)
    {
        float angle = (float)(random.NextDouble() * 360.0);
        outgoing = PacketCodec.EncodeShoot(sequenceNumber, new ShootPayload(angle));
        description = $"SHOOT угол={angle:F1}°";
    }
    else
    {
        x += (float)(random.NextDouble() * 2 - 1);
        y += (float)(random.NextDouble() * 2 - 1);
        outgoing = PacketCodec.EncodeMovement(sequenceNumber, new MovementPayload(x, y));
        description = $"MOVEMENT -> ({x:F2}, {y:F2})";
    }

    await udpClient.SendAsync(outgoing, outgoing.Length);
    Console.WriteLine($"[seq={sequenceNumber}] Отправлено: {description}");

    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result = await udpClient.ReceiveAsync(cts.Token);
        var (header, payload) = PacketCodec.Decode(result.Buffer);

        if (header.Command == CommandType.StateAck)
        {
            var ack = StateAckPayload.ReadFrom(payload.Span);
            Console.WriteLine($"  <- Ответ сервера: подтверждён seq={ack.AckedSequenceNumber}, позиция ({ack.X:F2}, {ack.Y:F2})");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("  <- Таймаут ожидания ответа сервера");
    }

    await Task.Delay(SendIntervalMs);
}
