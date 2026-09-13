namespace Telemetry;

/// <summary>Результат обработки одного входящего PONG.</summary>
/// <param name="RttMs">Заполнено только для <see cref="ResponseStatus.OnTime"/>.</param>
/// <param name="SmoothedRttMs">Актуальное значение SRTT после обработки (только для OnTime).</param>
/// <param name="JitterMs">Актуальное значение джиттера после обработки (только для OnTime).</param>
public readonly record struct ResponseOutcome(
    uint SequenceNumber,
    ResponseStatus Status,
    double? RttMs = null,
    double? SmoothedRttMs = null,
    double? JitterMs = null);
