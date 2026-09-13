using Telemetry;
using Xunit;

namespace Tests;

public class SmoothedRttCalculatorTests
{
    [Fact]
    public void FirstSample_SrttEqualsSample_RttVarIsHalfSample()
    {
        var calc = new SmoothedRttCalculator();
        calc.Update(100.0);

        Assert.Equal(100.0, calc.SmoothedRttMs, precision: 6);
        Assert.Equal(50.0, calc.RttVariationMs, precision: 6);
    }

    [Fact]
    public void SubsequentSamples_MatchRfc6298Formula()
    {
        // Ручной расчёт по RFC 6298 (alpha=1/8, beta=1/4) для сверки с реализацией.
        var calc = new SmoothedRttCalculator();

        calc.Update(100.0); // SRTT=100, RTTVAR=50
        double srtt2 = calc.Update(120.0); // RTTVAR=42.5, SRTT=102.5
        double rttVarAfter2 = calc.RttVariationMs;
        double srtt3 = calc.Update(90.0);  // RTTVAR=35.0, SRTT=100.9375
        double rttVarAfter3 = calc.RttVariationMs;

        Assert.Equal(102.5, srtt2, precision: 6);
        Assert.Equal(42.5, rttVarAfter2, precision: 6);

        Assert.Equal(100.9375, srtt3, precision: 6);
        Assert.Equal(35.0, rttVarAfter3, precision: 6);
    }

    [Fact]
    public void ConstantRtt_SrttConvergesToThatValue()
    {
        // При постоянном RTT |SRTT-R|=0 после первого замера, поэтому RTTVAR убывает
        // геометрически (x0.75 за шаг) и стремится к нулю, но не достигает его точно.
        var calc = new SmoothedRttCalculator();
        double result = 0;
        for (int i = 0; i < 50; i++)
            result = calc.Update(75.0);

        Assert.Equal(75.0, result, precision: 6);
        Assert.True(calc.RttVariationMs < 0.001, $"Ожидалось RTTVAR ~0, получено {calc.RttVariationMs}");
    }
}
