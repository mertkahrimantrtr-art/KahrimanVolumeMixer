using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

public class MasterVolumeControllerTests
{
    [Fact]
    public void Attach_AdoptsWindowsLevel_AndNeverAppliesSavedLevel()
    {
        // Kayıtlı seviye %75, Windows %42 → mikser %42 göstermeli, Windows değişmemeli.
        var c = new MasterVolumeController(initialLevel: 0.75);
        var device = new FakeEndpoint("spk", 0.42f);

        Assert.True(c.Attach(device));

        Assert.Equal(0.42, c.Level, 4);
        Assert.Equal(0.42, device.Level, 4);
        Assert.Equal(0, device.WriteCount);
        Assert.True(c.IsConnected);
    }

    [Fact]
    public void Attach_AdoptsMuteState()
    {
        var c = new MasterVolumeController();
        Assert.True(c.Attach(new FakeEndpoint("spk", 0.3f, mute: true)));
        Assert.True(c.IsMuted);
    }

    [Fact]
    public void SetLevel_WritesToWindows()
    {
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.42f);
        c.Attach(device);

        c.SetLevel(0.80);
        Assert.Equal(0.80, device.Level, 4);

        c.SetLevel(0);
        Assert.Equal(0.0, device.Level, 4);
    }

    [Fact]
    public void ExternalChange_IsDetected()
    {
        // Windows'tan %25'e düşürüldü → mikser %25 göstermeli.
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.42f);
        c.Attach(device);

        device.Level = 0.25f;
        Assert.True(c.DetectExternalChanges());
        Assert.Equal(0.25, c.Level, 4);
        Assert.False(c.DetectExternalChanges());
    }

    [Fact]
    public void OwnWrites_AreNotReportedAsExternal()
    {
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.5f);
        c.Attach(device);

        for (int i = 0; i <= 100; i++)
        {
            c.SetLevel(i / 100.0);
            Assert.False(c.DetectExternalChanges());
        }
    }

    [Fact]
    public void ExternalMute_IsDetected()
    {
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.5f);
        c.Attach(device);

        device.Mute = true;
        Assert.True(c.DetectExternalChanges());
        Assert.True(c.IsMuted);

        device.Mute = false;
        Assert.True(c.DetectExternalChanges());
        Assert.False(c.IsMuted);
    }

    [Fact]
    public void Mute_KeepsLevel()
    {
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.7f);
        c.Attach(device);

        c.SetMuted(true);
        Assert.True(device.Mute);
        Assert.Equal(0.7, device.Level, 4);

        c.SetMuted(false);
        Assert.False(device.Mute);
        Assert.Equal(0.7, device.Level, 4);
    }

    [Fact]
    public void DeviceSwitch_AdoptsNewDeviceLevel()
    {
        var c = new MasterVolumeController();
        var speakers = new FakeEndpoint("spk", 0.6f);
        var headphones = new FakeEndpoint("hp", 0.2f);
        c.Attach(speakers);

        Assert.True(c.Attach(headphones));
        Assert.Equal("hp", c.EndpointId);
        Assert.Equal(0.2, c.Level, 4);
        Assert.Equal(0.6, speakers.Level, 4);
        Assert.Equal(0, headphones.WriteCount);
    }

    [Fact]
    public void NoDevice_DoesNotThrow()
    {
        var c = new MasterVolumeController(0.5);
        Assert.False(c.Attach(null));
        Assert.False(c.IsConnected);

        c.SetLevel(0.3);
        c.SetMuted(true);
        Assert.Equal(0.3, c.Level, 6);
        Assert.False(c.DetectExternalChanges());
    }

    [Fact]
    public void RemovedDevice_Disconnects_WithoutThrowing()
    {
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.5f);
        c.Attach(device);

        device.Dead = true;
        Assert.True(c.DetectExternalChanges());
        Assert.False(c.IsConnected);

        var c2 = new MasterVolumeController();
        var d2 = new FakeEndpoint("spk", 0.5f);
        c2.Attach(d2);
        d2.Dead = true;
        c2.SetLevel(0.9);
        Assert.False(c2.IsConnected);
    }

    [Fact]
    public void DeadDeviceOnAttach_IsRejected()
    {
        var c = new MasterVolumeController();
        Assert.False(c.Attach(new FakeEndpoint("spk") { Dead = true }));
        Assert.False(c.IsConnected);
    }

    [Fact]
    public void Level_IsClamped()
    {
        var c = new MasterVolumeController();
        var device = new FakeEndpoint("spk", 0.5f);
        c.Attach(device);

        c.SetLevel(1.7);
        Assert.Equal(1.0, c.Level, 6);
        Assert.Equal(1f, device.Level);

        c.SetLevel(-3);
        Assert.Equal(0.0, c.Level, 6);
        Assert.Equal(0f, device.Level);

        c.SetLevel(double.NaN);
        Assert.Equal(0.0, c.Level, 6);
    }
}
