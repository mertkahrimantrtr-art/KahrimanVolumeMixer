using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

public class VolumeRampTests
{
    private static int FramesToFinish(VolumeRamp ramp, double frameMs = 16, int limit = 1000)
    {
        int frames = 0;
        while (ramp.IsActive && frames < limit)
        {
            ramp.Advance(frameMs);
            frames++;
        }

        return frames;
    }

    [Fact]
    public void ReachesTarget_WithinAboutTheConfiguredDuration()
    {
        var ramp = new VolumeRamp(0) { DurationMs = 150 };
        ramp.SetTarget(1);
        int frames = FramesToFinish(ramp);

        Assert.Equal(1.0, ramp.Current);
        Assert.InRange(frames * 16, 80, 400);
    }

    [Fact]
    public void Decrease_IsFasterThanIncrease()
    {
        var up = new VolumeRamp(0) { DurationMs = 200 };
        up.SetTarget(1);
        int upFrames = FramesToFinish(up);

        var down = new VolumeRamp(1) { DurationMs = 200 };
        down.SetTarget(0);
        int downFrames = FramesToFinish(down);

        Assert.True(downFrames < upFrames, $"down={downFrames} up={upFrames}");
    }

    [Fact]
    public void IsMonotonic_AndNeverOvershoots()
    {
        var ramp = new VolumeRamp(0.2) { DurationMs = 150 };
        ramp.SetTarget(0.9);
        double prev = ramp.Current;
        while (ramp.IsActive)
        {
            ramp.Advance(7);
            Assert.True(ramp.Current >= prev);
            Assert.True(ramp.Current <= 0.9);
            prev = ramp.Current;
        }
    }

    [Fact]
    public void Retarget_MidTransition_HeadsToNewTarget()
    {
        var ramp = new VolumeRamp(0.5) { DurationMs = 150 };
        ramp.SetTarget(1.0);
        ramp.Advance(16);
        double mid = ramp.Current;
        Assert.True(mid > 0.5);

        ramp.SetTarget(0.1);
        ramp.Advance(16);
        Assert.True(ramp.Current < mid);

        FramesToFinish(ramp);
        Assert.Equal(0.1, ramp.Current, 9);
    }

    [Fact]
    public void FirstFrame_ProducesAudibleChange()
    {
        var ramp = new VolumeRamp(0.8) { DurationMs = 150 };
        ramp.SetTarget(0.2);
        ramp.Advance(16);
        Assert.True(0.8 - ramp.Current > 0.1, $"current={ramp.Current}");
    }

    [Fact]
    public void Disabled_JumpsImmediately()
    {
        var ramp = new VolumeRamp(0) { Enabled = false };
        ramp.SetTarget(0.7);
        Assert.Equal(0.7, ramp.Current, 9);
        Assert.False(ramp.IsActive);
    }

    [Fact]
    public void Target_IsClamped()
    {
        var ramp = new VolumeRamp(0.5);
        ramp.SetTarget(5);
        Assert.Equal(1.0, ramp.Target);
        ramp.SetTarget(-5);
        Assert.Equal(0.0, ramp.Target);
    }

    [Fact]
    public void LongStall_DoesNotJumpPastTarget()
    {
        var ramp = new VolumeRamp(0) { DurationMs = 150 };
        ramp.SetTarget(1);
        ramp.Advance(10_000);
        Assert.InRange(ramp.Current, 0.0, 1.0);
    }

    [Fact]
    public void ZeroOrNegativeElapsed_DoesNothing()
    {
        var ramp = new VolumeRamp(0) { DurationMs = 150 };
        ramp.SetTarget(1);
        ramp.Advance(0);
        ramp.Advance(-5);
        Assert.Equal(0.0, ramp.Current);
    }
}
