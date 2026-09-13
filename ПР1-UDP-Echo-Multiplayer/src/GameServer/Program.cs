using System.Net;
using System.Net.Sockets;
using GameProtocol;

const int Port = 5055;
const string LogPath = "server.log";

var playerPositions = new Dictionary<string, PlayerPosition>();
using var udpServer = new UdpClient(Port);
using var logWriter = new StreamWriter(LogPath, append: true) { AutoFlush = true };

Log($"Сервер запущен, порт {Port}");
Console.WriteLine($"UDP-сервер слушает порт {Port}. Ctrl+C для остановки.");

while (true)
{
    UdpReceiveResult received;
    try
    {
        received = await udpServer.ReceiveAsync();
    }
    catch (SocketException ex)
    {
        Log($"Ошибка сокета при приёме: {ex.Message}");
        continue;
    }

    var clientKey = received.RemoteEndPoint.ToString();

    PacketHeader header;
    ReadOnlyMemory<byte> payload;
    try
    {
        (header, payload) = PacketCodec.Decode(received.Buffer);
    }
    catch (FormatException ex)
    {
        Log($"[{clientKey}] Отброшен некорректный пакет: {ex.Message}");
        continue;
    }

    switch (header.Command)
    {
        case CommandType.Movement:
        {
            var move = MovementPayload.ReadFrom(payload.Span);
            playerPositions[clientKey] = new PlayerPosition(move.X, move.Y);
            Log($"[{clientKey}] MOVEMENT seq={header.SequenceNumber} -> ({move.X:F2}, {move.Y:F2})");

            var ack = PacketCodec.EncodeStateAck(header.SequenceNumber, new StateAckPayload(header.SequenceNumber, move.X, move.Y));
            await udpServer.SendAsync(ack, ack.Length, received.RemoteEndPoint);
            break;
        }
        case CommandType.Shoot:
        {
            var shoot = ShootPayload.ReadFrom(payload.Span);
            var pos = playerPositions.GetValueOrDefault(clientKey, new PlayerPosition(0f, 0f));
            Log($"[{clientKey}] SHOOT seq={header.SequenceNumber} угол={shoot.AngleDegrees:F1}° из позиции ({pos.X:F2}, {pos.Y:F2})");

            var ack = PacketCodec.EncodeStateAck(header.SequenceNumber, new StateAckPayload(header.SequenceNumber, pos.X, pos.Y));
            await udpServer.SendAsync(ack, ack.Length, received.RemoteEndPoint);
            break;
        }
        default:
            Log($"[{clientKey}] Неизвестный тип команды: {header.Command}");
            break;
    }
}

void Log(string message)
{
    var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {message}";
    Console.WriteLine(line);
    logWriter.WriteLine(line);
}

readonly record struct PlayerPosition(float X, float Y);
