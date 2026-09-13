using System.Buffers.Binary;

namespace GameProtocol;

/// <summary>
/// Сборка и разбор пакетов вида [PacketHeader][Payload][CRC32:4].
/// Контрольная сумма считается по заголовку и payload вместе и проверяется при декодировании
/// (доп. задание: обнаружение повреждённых пакетов, т.к. UDP целостность не гарантирует).
/// </summary>
public static class PacketCodec
{
    private const int ChecksumSize = 4;

    public static byte[] EncodeMovement(uint sequenceNumber, MovementPayload payload) =>
        Encode(CommandType.Movement, sequenceNumber, MovementPayload.Size, payload.WriteTo);

    public static byte[] EncodeShoot(uint sequenceNumber, ShootPayload payload) =>
        Encode(CommandType.Shoot, sequenceNumber, ShootPayload.Size, payload.WriteTo);

    public static byte[] EncodeStateAck(uint sequenceNumber, StateAckPayload payload) =>
        Encode(CommandType.StateAck, sequenceNumber, StateAckPayload.Size, payload.WriteTo);

    private static byte[] Encode(CommandType command, uint sequenceNumber, ushort payloadSize, SpanAction writePayload)
    {
        var buffer = new byte[PacketHeader.Size + payloadSize + ChecksumSize];
        new PacketHeader(command, sequenceNumber, payloadSize).WriteTo(buffer);
        writePayload(buffer.AsSpan(PacketHeader.Size, payloadSize));

        uint crc = Crc32.Compute(buffer.AsSpan(0, PacketHeader.Size + payloadSize));
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(PacketHeader.Size + payloadSize, ChecksumSize), crc);
        return buffer;
    }

    private delegate void SpanAction(Span<byte> destination);

    public static (PacketHeader Header, ReadOnlyMemory<byte> Payload) Decode(ReadOnlyMemory<byte> datagram)
    {
        if (datagram.Length < PacketHeader.Size + ChecksumSize)
            throw new FormatException($"Датаграмма короче заголовка+CRC: {datagram.Length} байт");

        var header = PacketHeader.ReadFrom(datagram.Span[..PacketHeader.Size]);
        int payloadEnd = PacketHeader.Size + header.PayloadSize;

        if (datagram.Length != payloadEnd + ChecksumSize)
            throw new FormatException(
                $"Заявленный размер payload ({header.PayloadSize}) не совпадает с фактическим ({datagram.Length - PacketHeader.Size - ChecksumSize})");

        uint expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(datagram.Span[payloadEnd..(payloadEnd + ChecksumSize)]);
        uint actualCrc = Crc32.Compute(datagram.Span[..payloadEnd]);
        if (expectedCrc != actualCrc)
            throw new FormatException($"Несовпадение CRC32: ожидалось {expectedCrc:X8}, получено {actualCrc:X8} — пакет повреждён");

        var payload = datagram[PacketHeader.Size..payloadEnd];
        return (header, payload);
    }
}
