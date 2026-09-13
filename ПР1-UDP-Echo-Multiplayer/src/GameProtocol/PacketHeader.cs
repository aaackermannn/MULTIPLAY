using System.Buffers.Binary;

namespace GameProtocol;

/// <summary>
/// Фиксированный заголовок пакета: тип команды, порядковый номер, размер полезной нагрузки.
/// Layout (7 байт, big-endian): [Command:1][SequenceNumber:4][PayloadSize:2]
/// </summary>
public readonly struct PacketHeader
{
    public const int Size = 7;

    public CommandType Command { get; }
    public uint SequenceNumber { get; }
    public ushort PayloadSize { get; }

    public PacketHeader(CommandType command, uint sequenceNumber, ushort payloadSize)
    {
        Command = command;
        SequenceNumber = sequenceNumber;
        PayloadSize = payloadSize;
    }

    public void WriteTo(Span<byte> buffer)
    {
        buffer[0] = (byte)Command;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..5], SequenceNumber);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[5..7], PayloadSize);
    }

    public static PacketHeader ReadFrom(ReadOnlySpan<byte> buffer)
    {
        var command = (CommandType)buffer[0];
        var sequenceNumber = BinaryPrimitives.ReadUInt32BigEndian(buffer[1..5]);
        var payloadSize = BinaryPrimitives.ReadUInt16BigEndian(buffer[5..7]);
        return new PacketHeader(command, sequenceNumber, payloadSize);
    }
}
