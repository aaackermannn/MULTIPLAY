using System.Net;
using System.Net.Sockets;

namespace Transport;

/// <summary>
/// Тонкая обёртка над UDP-сокетом: только приём/отправка сырых датаграмм.
/// Не знает ни о формате пакетов (Protocol), ни о расчёте метрик (Telemetry).
/// </summary>
public sealed class UdpTransport : IDisposable
{
    private readonly UdpClient _socket;

    /// <summary>Создать транспорт, слушающий указанный локальный порт (роль сервера).</summary>
    public UdpTransport(int localPort) => _socket = new UdpClient(localPort);

    /// <summary>Создать транспорт с эфемерным локальным портом (роль клиента).</summary>
    public UdpTransport() => _socket = new UdpClient(0);

    public Task SendAsync(byte[] datagram, IPEndPoint remoteEndPoint) =>
        _socket.SendAsync(datagram, datagram.Length, remoteEndPoint);

    public async Task<(byte[] Data, IPEndPoint RemoteEndPoint)> ReceiveAsync(CancellationToken cancellationToken)
    {
        var result = await _socket.ReceiveAsync(cancellationToken);
        return (result.Buffer, result.RemoteEndPoint);
    }

    public void Dispose() => _socket.Dispose();
}
