using Telemetry;
using Xunit;

namespace Tests;

public class JitterCalculatorTests
{
    [Fact]
    public void FirstSample_JitterStaysZero_NoPreviousToCompareAgainst()
    {
        var calc = new JitterCalculator();
        double jitter = calc.Update(100.0);
        Assert.Equal(0.0, jitter, precision: 6);
    }

    [Fact]
    public void SubsequentSamples_MatchRfc3550Formula()
    {
        // J = J + (|D| - J) / 16, D — разница соседних RTT. Ручной расчёт для сверки.
        var calc = new JitterCalculator();

        calc.Update(100.0);                 // J=0 (базовая точка)
        double j2 = calc.Update(120.0);      // D=20, J=20/16=1.25
        double j3 = calc.Update(90.0);       // D=30, J=1.25+(30-1.25)/16=3.046875

        Assert.Equal(1.25, j2, precision: 6);
        Assert.Equal(3.046875, j3, precision: 6);
    }

    [Fact]
    public void ConstantRtt_JitterConvergesToZero()
    {
        var calc = new JitterCalculator();
        calc.Update(50.0);
        double jitter = 0;
        for (int i = 0; i < 20; i++)
            jitter = calc.Update(50.0);

        Assert.Equal(0.0, jitter, precision: 6);
    }
}
