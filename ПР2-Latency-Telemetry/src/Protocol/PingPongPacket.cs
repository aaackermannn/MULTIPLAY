namespace Protocol;

/// <summary>
/// PING и PONG используют одинаковую структуру: сервер эхом возвращает клиентскую метку
/// времени, поэтому клиент считает RTT по своим часам — синхронизация часов не нужна.
/// </summary>
/// <param name="Type">Ping или Pong.</param>
/// <param name="SequenceNumber">Порядковый номер, генерируется клиентом при отправке PING.</param>
/// <param name="ClientTimestampTicks">DateTime.UtcNow.Ticks клиента в момент отправки PING.</param>
public readonly record struct PingPongPacket(PacketType Type, uint SequenceNumber, long ClientTimestampTicks);
