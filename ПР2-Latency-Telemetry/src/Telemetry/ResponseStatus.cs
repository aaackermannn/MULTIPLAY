namespace Telemetry;

public enum ResponseStatus
{
    /// <summary>Ответ пришёл впервые и в пределах таймаута — учитывается в RTT/SRTT/джиттере.</summary>
    OnTime,

    /// <summary>Ответ на уже помеченный потерянным (по таймауту) запрос — пришёл, но поздно.</summary>
    Late,

    /// <summary>Повторный ответ на уже подтверждённый запрос.</summary>
    Duplicate,

    /// <summary>Ответ ссылается на номер, который не отправлялся (испорченный/чужой/поддельный пакет).</summary>
    Unknown,

    /// <summary>Ответ не пришёл до истечения таймаута (фиксируется через <see cref="LatencyTracker.Tick"/>).</summary>
    Lost
}
