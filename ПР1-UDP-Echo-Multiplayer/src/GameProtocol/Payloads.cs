using System.Buffers.Binary;

namespace GameProtocol;

/// <summary>Координаты игрока после перемещения.</summary>
public readonly struct MovementPayload
{
    public const int Size = 8; // float X + float Y

    public float X { get; }
    public float Y { get; }

    public MovementPayload(float x, float y)
    {
        X = x;
        Y = y;
    }

    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleBigEndian(buffer[0..4], X);
        BinaryPrimitives.WriteSingleBigEndian(buffer[4..8], Y);
    }

    public static MovementPayload ReadFrom(ReadOnlySpan<byte> buffer)
    {
        float x = BinaryPrimitives.ReadSingleBigEndian(buffer[0..4]);
        float y = BinaryPrimitives.ReadSingleBigEndian(buffer[4..8]);
        return new MovementPayload(x, y);
    }
}

/// <summary>Имитация выстрела: угол направления в градусах.</summary>
public readonly struct ShootPayload
{
    public const int Size = 4; // float AngleDegrees

    public float AngleDegrees { get; }

    public ShootPayload(float angleDegrees)
    {
        AngleDegrees = angleDegrees;
    }

    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleBigEndian(buffer[0..4], AngleDegrees);
    }

    public static ShootPayload ReadFrom(ReadOnlySpan<byte> buffer)
    {
        float angle = BinaryPrimitives.ReadSingleBigEndian(buffer[0..4]);
        return new ShootPayload(angle);
    }
}

/// <summary>Ответ сервера: подтверждение с актуальным состоянием позиции игрока.</summary>
public readonly struct StateAckPayload
{
    public const int Size = 12; // uint AckedSequence + float X + float Y

    public uint AckedSequenceNumber { get; }
    public float X { get; }
    public float Y { get; }

    public StateAckPayload(uint ackedSequenceNumber, float x, float y)
    {
        AckedSequenceNumber = ackedSequenceNumber;
        X = x;
        Y = y;
    }

    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer[0..4], AckedSequenceNumber);
        BinaryPrimitives.WriteSingleBigEndian(buffer[4..8], X);
        BinaryPrimitives.WriteSingleBigEndian(buffer[8..12], Y);
    }

    public static StateAckPayload ReadFrom(ReadOnlySpan<byte> buffer)
    {
        uint ackedSeq = BinaryPrimitives.ReadUInt32BigEndian(buffer[0..4]);
        float x = BinaryPrimitives.ReadSingleBigEndian(buffer[4..8]);
        float y = BinaryPrimitives.ReadSingleBigEndian(buffer[8..12]);
        return new StateAckPayload(ackedSeq, x, y);
    }
}
