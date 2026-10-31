using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

/// <summary>Gerçek WASAPI oturumunun yerine geçen test oturumu.</summary>
internal sealed class FakeSession : IVolumeSession
{
    public FakeSession(string key, float volume = 1f, bool mute = false)
    {
        Key = key;
        Volume = volume;
        Mute = mute;
    }

    public string Key { get; }
    public float Volume { get; set; }
    public bool Mute { get; set; }
    public bool Dead { get; set; }
    public int WriteCount { get; private set; }

    public float GetVolume() => Dead ? throw new InvalidOperationException("dead") : Volume;

    public void SetVolume(float volume)
    {
        if (Dead) throw new InvalidOperationException("dead");
        Volume = volume;
        WriteCount++;
    }

    public bool GetMute() => Dead ? throw new InvalidOperationException("dead") : Mute;

    public void SetMute(bool mute)
    {
        if (Dead) throw new InvalidOperationException("dead");
        Mute = mute;
    }
}
