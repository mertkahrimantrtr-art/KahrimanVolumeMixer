using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

/// <summary>Windows ses aygıtının yerine geçen test aygıtı.</summary>
internal sealed class FakeEndpoint : IVolumeEndpoint
{
    public FakeEndpoint(string id, float level = 0.5f, bool mute = false)
    {
        Id = id;
        Level = level;
        Mute = mute;
    }

    public string Id { get; }
    public float Level { get; set; }
    public bool Mute { get; set; }
    public bool Dead { get; set; }
    public int WriteCount { get; private set; }

    public float GetLevel() => Dead ? throw new InvalidOperationException("removed") : Level;

    public void SetLevel(float level)
    {
        if (Dead) throw new InvalidOperationException("removed");
        Level = level;
        WriteCount++;
    }

    public bool GetMute() => Dead ? throw new InvalidOperationException("removed") : Mute;

    public void SetMute(bool mute)
    {
        if (Dead) throw new InvalidOperationException("removed");
        Mute = mute;
    }
}
