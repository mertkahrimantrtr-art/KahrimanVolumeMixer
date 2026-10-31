using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

public class SessionGroupControllerTests
{
    private const double Tol = 0.0001;

    [Fact]
    public void Disconnected_SetMaster_DoesNotThrow_AndIsPending()
    {
        var c = new SessionGroupController(0.5);
        c.SetMaster(0.3);
        c.SetMuted(true);
        Assert.False(c.IsConnected);
        Assert.True(c.HasPendingUserIntent);
        Assert.Equal(0.3, c.Master, 6);
        Assert.False(c.DetectExternalChanges());
    }

    [Fact]
    public void FirstConnection_AdoptsChromeLevel()
    {
        var c = new SessionGroupController(0.75);
        var s = new FakeSession("a", 0.4f);

        bool adopted = c.Sync(new[] { s });

        Assert.True(adopted);
        Assert.True(c.IsConnected);
        Assert.Equal(0.4, c.Master, 4);
        Assert.Equal(0.4, s.Volume, 4);
    }

    [Fact]
    public void FirstConnection_AppliesUserLevel_WhenChangedWhileDisconnected()
    {
        var c = new SessionGroupController(0.75);
        c.SetMaster(0.2);
        var s = new FakeSession("a", 1f);

        bool adopted = c.Sync(new[] { s });

        Assert.False(adopted);
        Assert.Equal(0.2, s.Volume, 4);
        Assert.False(c.HasPendingUserIntent);
    }

    [Fact]
    public void SetMaster_ControlsAllSessions_PreservingRelativeBalance()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.8f);
        var b = new FakeSession("b", 0.4f);
        c.Sync(new[] { a, b });

        Assert.Equal(0.8, c.Master, 4);
        c.SetMaster(0.5);

        Assert.Equal(0.5, a.Volume, 4);
        Assert.Equal(0.25, b.Volume, 4);
    }

    [Fact]
    public void Balance_SurvivesGoingToZeroAndBack()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 1f);
        var b = new FakeSession("b", 0.5f);
        c.Sync(new[] { a, b });

        c.SetMaster(0);
        Assert.Equal(0f, a.Volume);
        Assert.Equal(0f, b.Volume);
        Assert.False(c.DetectExternalChanges());

        c.SetMaster(0.6);
        Assert.Equal(0.6, a.Volume, 4);
        Assert.Equal(0.3, b.Volume, 4);
    }

    [Fact]
    public void NewSession_WhileConnected_IsSyncedToMasterAndMute()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.6f);
        c.Sync(new[] { a });
        c.SetMuted(true);

        var b = new FakeSession("b", 1f);
        bool adopted = c.Sync(new[] { a, b });

        Assert.False(adopted);
        Assert.Equal(2, c.SessionCount);
        Assert.Equal(0.6, b.Volume, 4);
        Assert.True(b.Mute);
    }

    [Fact]
    public void RemovedSessions_AreDropped_AndReconnectAdoptsAgain()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.6f);
        c.Sync(new[] { a });

        c.Sync(Array.Empty<IVolumeSession>());
        Assert.False(c.IsConnected);

        var a2 = new FakeSession("a2", 0.3f);
        Assert.True(c.Sync(new[] { a2 }));
        Assert.Equal(0.3, c.Master, 4);
    }

    [Fact]
    public void ExternalVolumeChange_IsDetected_AndBalanceRecaptured()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.5f);
        var b = new FakeSession("b", 0.5f);
        c.Sync(new[] { a, b });

        // Windows ses mikserinden yalnızca b değiştirildi
        b.Volume = 0.25f;
        Assert.True(c.DetectExternalChanges());
        Assert.Equal(0.5, c.Master, 4);
        Assert.Equal(0.5, c.GetRatios()["b"], 3);

        // Uygulama sesi Chrome geneli artırıldı
        a.Volume = 0.9f;
        b.Volume = 0.45f;
        Assert.True(c.DetectExternalChanges());
        Assert.Equal(0.9, c.Master, 4);

        Assert.False(c.DetectExternalChanges());
    }

    [Fact]
    public void OwnWrites_AreNotReportedAsExternal()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.5f);
        c.Sync(new[] { a });

        for (int i = 0; i <= 100; i++)
        {
            c.SetMaster(i / 100.0);
            Assert.False(c.DetectExternalChanges());
        }
    }

    [Fact]
    public void ExternalMute_IsDetected()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.5f);
        c.Sync(new[] { a });

        a.Mute = true;
        Assert.True(c.DetectExternalChanges());
        Assert.True(c.IsMuted);

        a.Mute = false;
        Assert.True(c.DetectExternalChanges());
        Assert.False(c.IsMuted);
    }

    [Fact]
    public void Mute_KeepsLevel()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.7f);
        c.Sync(new[] { a });

        c.SetMuted(true);
        Assert.True(a.Mute);
        Assert.Equal(0.7, a.Volume, 4);

        c.SetMuted(false);
        Assert.False(a.Mute);
        Assert.Equal(0.7, a.Volume, 4);
    }

    [Fact]
    public void DeadSession_IsRemoved_WithoutThrowing()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.5f);
        var b = new FakeSession("b", 0.5f);
        c.Sync(new[] { a, b });

        b.Dead = true;
        c.SetMaster(0.2);

        Assert.Equal(1, c.SessionCount);
        Assert.Equal(0.2, a.Volume, 4);
    }

    [Fact]
    public void DuplicateKeys_AreIgnored()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.5f);
        c.Sync(new IVolumeSession[] { a, a });
        Assert.Equal(1, c.SessionCount);
    }

    [Fact]
    public void Master_IsClamped()
    {
        var c = new SessionGroupController(0);
        var a = new FakeSession("a", 0.5f);
        c.Sync(new[] { a });

        c.SetMaster(1.7);
        Assert.Equal(1.0, c.Master, 6);
        Assert.Equal(1f, a.Volume);

        c.SetMaster(-3);
        Assert.Equal(0.0, c.Master, 6);
        Assert.Equal(0f, a.Volume);

        c.SetMaster(double.NaN);
        Assert.Equal(0.0, c.Master, 6);
        Assert.True(Math.Abs(a.Volume) < Tol);
    }
}
