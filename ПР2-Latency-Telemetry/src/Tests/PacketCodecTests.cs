using Protocol;
using Xunit;

namespace Tests;

public class PacketCodecTests
{
    [Theory]
    [InlineData(PacketType.Ping, 1u, 123456789L)]
    [InlineData(PacketType.Pong, 4294967295u, long.MaxValue)]
    [InlineData(PacketType.Ping, 0u, long.MinValue)]
    public void EncodeThenDecode_ReturnsOriginalValues(PacketType type, uint sequence, long timestamp)
    {
        var original = new PingPongPacket(type, sequence, timestamp);

        byte[] encoded = PacketCodec.Encode(original);
        var decoded = PacketCodec.Decode(encoded);

        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Encode_ProducesFixedSizePacket()
    {
        var packet = new PingPongPacket(PacketType.Ping, 42, DateTime.UtcNow.Ticks);
        byte[] encoded = PacketCodec.Encode(packet);
        Assert.Equal(PacketCodec.PacketSize, encoded.Length);
    }

    [Fact]
    public void Decode_TooShortDatagram_ThrowsMalformedPacketException()
    {
        var tooShort = new byte[PacketCodec.PacketSize - 1];
        Assert.Throws<MalformedPacketException>(() => PacketCodec.Decode(tooShort));
    }

    [Fact]
    public void Decode_TooLongDatagram_ThrowsMalformedPacketException()
    {
        var tooLong = new byte[PacketCodec.PacketSize + 5];
        Assert.Throws<MalformedPacketException>(() => PacketCodec.Decode(tooLong));
    }

    [Fact]
    public void Decode_UnknownPacketType_ThrowsMalformedPacketException()
    {
        byte[] buffer = PacketCodec.Encode(new PingPongPacket(PacketType.Ping, 1, 0));
        buffer[0] = 0xFF; // несуществующий тип пакета

        Assert.Throws<MalformedPacketException>(() => PacketCodec.Decode(buffer));
    }

    [Fact]
    public void Decode_EmptyDatagram_ThrowsMalformedPacketException()
    {
        Assert.Throws<MalformedPacketException>(() => PacketCodec.Decode(ReadOnlySpan<byte>.Empty));
    }
}
