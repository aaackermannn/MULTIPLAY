using System.Buffers.Binary;

namespace Protocol;

/// <summary>
/// Кодирование/декодирование PING/PONG-пакетов. Только сериализация — никакого сетевого
/// ввода-вывода и никакой логики расчёта метрик (это ответственность Transport и Telemetry).
/// Формат (13 байт, big-endian): [Type:1][SequenceNumber:4][ClientTimestampTicks:8]
/// </summary>
public static class PacketCodec
{
    public const int PacketSize = 1 + 4 + 8;

    public static byte[] Encode(PingPongPacket packet)
    {
        var buffer = new byte[PacketSize];
        buffer[0] = (byte)packet.Type;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(1, 4), packet.SequenceNumber);
        BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(5, 8), packet.ClientTimestampTicks);
        return buffer;
    }

    public static PingPongPacket Decode(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length != PacketSize)
            throw new MalformedPacketException(
                $"Неверный размер пакета: ожидалось {PacketSize} байт, получено {datagram.Length}");

        byte typeByte = datagram[0];
        if (!Enum.IsDefined(typeof(PacketType), typeByte))
            throw new MalformedPacketException($"Неизвестный тип пакета: 0x{typeByte:X2}");

        uint sequence = BinaryPrimitives.ReadUInt32BigEndian(datagram.Slice(1, 4));
        long timestamp = BinaryPrimitives.ReadInt64BigEndian(datagram.Slice(5, 8));
        return new PingPongPacket((PacketType)typeByte, sequence, timestamp);
    }
}
