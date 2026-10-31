using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

public class WheelAcceleratorTests
{
    [Fact]
    public void SlowMovement_UsesSlowStep()
    {
        var a = new WheelAccelerator();
        long t = 1000;
        for (int i = 0; i < 5; i++)
        {
            double d = a.ComputeDeltaPercent(120, t);
            Assert.Equal(a.SlowStepPercent, d, 6);
            t += 300; // her çentik arası 300 ms
        }
    }

    [Fact]
    public void NormalMovement_UsesNormalStep()
    {
        var a = new WheelAccelerator();
        long t = 1000;
        double last = 0;
        for (int i = 0; i < 6; i++)
        {
            last = a.ComputeDeltaPercent(120, t);
            t += 100; // 10 çentik/sn
        }

        Assert.Equal(a.NormalStepPercent, last, 6);
    }

    [Fact]
    public void FastMovement_UsesFastStep()
    {
        var a = new WheelAccelerator();
        long t = 1000;
        double last = 0;
        for (int i = 0; i < 6; i++)
        {
            last = a.ComputeDeltaPercent(120, t);
            t += 20; // 50 çentik/sn
        }

        Assert.Equal(a.FastStepPercent, last, 6);
    }

    [Fact]
    public void DownwardMovement_IsNegative()
    {
        var a = new WheelAccelerator();
        Assert.True(a.ComputeDeltaPercent(-120, 0) < 0);
    }

    [Fact]
    public void DirectionChange_ResetsAcceleration()
    {
        var a = new WheelAccelerator();
        long t = 0;
        for (int i = 0; i < 6; i++) { a.ComputeDeltaPercent(120, t); t += 20; }

        double reversed = a.ComputeDeltaPercent(-120, t);
        Assert.Equal(-a.SlowStepPercent, reversed, 6);
    }

    [Fact]
    public void Disabled_UsesBaseStep()
    {
        var a = new WheelAccelerator { Enabled = false, BaseStepPercent = 3 };
        long t = 0;
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(3.0, a.ComputeDeltaPercent(120, t), 6);
            t += 10;
        }
    }

    [Fact]
    public void HighResolutionDelta_IsProportional()
    {
        var a = new WheelAccelerator();
        Assert.Equal(a.SlowStepPercent / 4.0, a.ComputeDeltaPercent(30, 0), 6);
    }

    [Fact]
    public void TimestampWrap_DoesNotBreak()
    {
        var a = new WheelAccelerator();
        a.ComputeDeltaPercent(120, int.MaxValue);
        double d = a.ComputeDeltaPercent(120, int.MinValue);
        Assert.Equal(a.SlowStepPercent, d, 6);
    }

    [Fact]
    public void ZeroDelta_ReturnsZero()
    {
        Assert.Equal(0.0, new WheelAccelerator().ComputeDeltaPercent(0, 0));
    }
}
