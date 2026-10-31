using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

public class LevelMemoryTests
{
    [Fact]
    public void Low_ThenLowAgain_ReturnsToPreviousLevel()
    {
        var m = new LevelMemory();
        double level = m.TogglePreset(0.75, 0.10);
        Assert.Equal(0.10, level, 6);

        level = m.TogglePreset(level, 0.10);
        Assert.Equal(0.75, level, 6);

        level = m.TogglePreset(level, 0.10);
        Assert.Equal(0.10, level, 6);
    }

    [Fact]
    public void LowAndHigh_CanBeToggled()
    {
        var m = new LevelMemory();
        double level = m.TogglePreset(0.10, 0.75); // HIGH
        Assert.Equal(0.75, level, 6);

        level = m.TogglePreset(level, 0.75); // HIGH tekrar → LOW'a dön
        Assert.Equal(0.10, level, 6);

        level = m.TogglePreset(level, 0.10); // LOW (zaten LOW) → HIGH'a dön
        Assert.Equal(0.75, level, 6);
    }

    [Fact]
    public void AtPreset_WithoutMemory_StaysAtPreset()
    {
        var m = new LevelMemory();
        Assert.Equal(0.75, m.TogglePreset(0.75, 0.75), 6);
    }

    [Fact]
    public void Inputs_AreClamped()
    {
        var m = new LevelMemory();
        Assert.Equal(1.0, m.TogglePreset(0.3, 1.5), 6);
        Assert.Equal(0.3, m.ReturnLevel!.Value, 6);
    }
}
