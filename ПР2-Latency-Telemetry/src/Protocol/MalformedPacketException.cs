namespace Protocol;

/// <summary>Датаграмма не является корректным PING/PONG-пакетом (короткая, неверный тип, обрезана).</summary>
public sealed class MalformedPacketException : Exception
{
    public MalformedPacketException(string message) : base(message) { }
}
