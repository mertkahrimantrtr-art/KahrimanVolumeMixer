namespace KahrimanVolumeMixer.Core;

public enum AppTheme
{
    Dark,
    Light,
}

/// <summary>Kalıcı uygulama ayarları ve son durum.</summary>
public sealed class AppSettings
{
    public int Version { get; set; } = 1;

    // --- Fare tekerleği ---
    public double WheelStepPercent { get; set; } = 2.0;
    public bool SmartAcceleration { get; set; } = true;
    public double SlowStepPercent { get; set; } = 1.5;
    public double NormalStepPercent { get; set; } = 5.0;
    public double FastStepPercent { get; set; } = 12.0;

    // --- Geçiş ---
    public bool SmoothTransitions { get; set; } = true;
    public double TransitionMs { get; set; } = 150.0;

    // --- Ön ayarlar ---
    public double LowLevelPercent { get; set; } = 10.0;
    public double HighLevelPercent { get; set; } = 75.0;

    // --- Pencere ---
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public double WindowOpacity { get; set; } = 0.96;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double WindowWidth { get; set; } = 100.0;
    public double WindowHeight { get; set; } = 300.0;

    // --- Son durum ---
    public double LastLevel { get; set; } = 0.75;
    public double? ReturnLevel { get; set; }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    /// <summary>Yalnızca kullanıcı tercihlerini kopyalar (pencere konumu ve son durum hariç).</summary>
    public void CopyPreferencesFrom(AppSettings other)
    {
        ArgumentNullException.ThrowIfNull(other);
        WheelStepPercent = other.WheelStepPercent;
        SmartAcceleration = other.SmartAcceleration;
        SlowStepPercent = other.SlowStepPercent;
        NormalStepPercent = other.NormalStepPercent;
        FastStepPercent = other.FastStepPercent;
        SmoothTransitions = other.SmoothTransitions;
        TransitionMs = other.TransitionMs;
        LowLevelPercent = other.LowLevelPercent;
        HighLevelPercent = other.HighLevelPercent;
        AlwaysOnTop = other.AlwaysOnTop;
        StartWithWindows = other.StartWithWindows;
        Theme = other.Theme;
        WindowOpacity = other.WindowOpacity;
        Normalize();
    }

    /// <summary>Geçersiz veya bozuk değerleri güvenli aralıklara çeker.</summary>
    public void Normalize()
    {
        var d = new AppSettings();
        WheelStepPercent = VolumeMath.Clamp(WheelStepPercent, 0.1, 50, d.WheelStepPercent);
        SlowStepPercent = VolumeMath.Clamp(SlowStepPercent, 0.1, 50, d.SlowStepPercent);
        NormalStepPercent = VolumeMath.Clamp(NormalStepPercent, 0.1, 50, d.NormalStepPercent);
        FastStepPercent = VolumeMath.Clamp(FastStepPercent, 0.1, 50, d.FastStepPercent);
        TransitionMs = VolumeMath.Clamp(TransitionMs, 0, 1000, d.TransitionMs);
        LowLevelPercent = VolumeMath.Clamp(LowLevelPercent, 0, 100, d.LowLevelPercent);
        HighLevelPercent = VolumeMath.Clamp(HighLevelPercent, 0, 100, d.HighLevelPercent);
        WindowOpacity = VolumeMath.Clamp(WindowOpacity, 0.3, 1.0, d.WindowOpacity);
        WindowWidth = VolumeMath.Clamp(WindowWidth, 76, 400, d.WindowWidth);
        WindowHeight = VolumeMath.Clamp(WindowHeight, 200, 1200, d.WindowHeight);
        LastLevel = VolumeMath.Clamp(LastLevel, 0, 1, d.LastLevel);

        if (WindowLeft is double left && (double.IsNaN(left) || double.IsInfinity(left))) WindowLeft = null;
        if (WindowTop is double top && (double.IsNaN(top) || double.IsInfinity(top))) WindowTop = null;
        if (ReturnLevel is double back) ReturnLevel = double.IsNaN(back) || double.IsInfinity(back) ? null : VolumeMath.Clamp01(back);
        if (!Enum.IsDefined(Theme)) Theme = AppTheme.Dark;
    }
}
