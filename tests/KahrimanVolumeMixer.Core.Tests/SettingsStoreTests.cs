using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "kvm-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_dir, "sub", "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temizlik */ }
    }

    [Fact]
    public void MissingFile_ReturnsDefaults()
    {
        var s = new SettingsStore(FilePath).Load();
        Assert.Equal(10, s.LowLevelPercent);
        Assert.Equal(75, s.HighLevelPercent);
        Assert.True(s.AlwaysOnTop);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var store = new SettingsStore(FilePath);
        var s = new AppSettings
        {
            LowLevelPercent = 15,
            HighLevelPercent = 80,
            SmartAcceleration = false,
            TransitionMs = 220,
            Theme = AppTheme.Light,
            WindowLeft = 1234.5,
            WindowTop = -200,
            LastLevel = 0.42,
            ReturnLevel = 0.9,
            StartWithWindows = true,
        };

        Assert.True(store.Save(s));
        var loaded = store.Load();

        Assert.Equal(15, loaded.LowLevelPercent);
        Assert.Equal(80, loaded.HighLevelPercent);
        Assert.False(loaded.SmartAcceleration);
        Assert.Equal(220, loaded.TransitionMs);
        Assert.Equal(AppTheme.Light, loaded.Theme);
        Assert.Equal(1234.5, loaded.WindowLeft);
        Assert.Equal(-200, loaded.WindowTop);
        Assert.Equal(0.42, loaded.LastLevel, 6);
        Assert.Equal(0.9, loaded.ReturnLevel);
        Assert.True(loaded.StartWithWindows);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void CorruptFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ bu geçerli json değil");
        var s = new SettingsStore(FilePath).Load();
        Assert.Equal(75, s.HighLevelPercent);
    }

    [Fact]
    public void OutOfRangeValues_AreNormalized()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ \"LowLevelPercent\": -50, \"HighLevelPercent\": 500, \"WindowOpacity\": 0.01, \"TransitionMs\": 99999, \"LastLevel\": 3 }");
        var s = new SettingsStore(FilePath).Load();
        Assert.Equal(0, s.LowLevelPercent);
        Assert.Equal(100, s.HighLevelPercent);
        Assert.Equal(0.3, s.WindowOpacity, 6);
        Assert.Equal(1000, s.TransitionMs);
        Assert.Equal(1.0, s.LastLevel, 6);
    }

    [Fact]
    public void CopyPreferencesFrom_DoesNotTouchWindowOrState()
    {
        var target = new AppSettings { WindowLeft = 10, LastLevel = 0.3 };
        var source = new AppSettings { WindowLeft = 999, LastLevel = 0.9, HighLevelPercent = 60, Theme = AppTheme.Light };

        target.CopyPreferencesFrom(source);

        Assert.Equal(10, target.WindowLeft);
        Assert.Equal(0.3, target.LastLevel, 6);
        Assert.Equal(60, target.HighLevelPercent);
        Assert.Equal(AppTheme.Light, target.Theme);
    }
}
