using System;
using System.Diagnostics;
using System.Windows.Threading;
using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Audio;

/// <summary>
/// Uygulamanın ses motoru: tekerlek → hızlandırma → hedef birikimi → yumuşak geçiş → Chrome oturumları.
/// Tüm işlemler UI iş parçacığında, düşük maliyetli zamanlayıcılarla çalışır:
///  • Geçiş zamanlayıcısı yalnızca bir geçiş sürerken çalışır (≈60 Hz).
///  • Yoklama zamanlayıcısı 250 ms'de bir dış değişiklikleri, ≈1 sn'de bir yeni/kapanan oturumları kontrol eder.
/// </summary>
internal sealed class MixerEngine : IDisposable
{
    private const int PollIntervalMs = 250;
    private const int SessionScanEveryNPolls = 4;
    private const double MinKickFrameMs = 8.0;

    private readonly ChromeAudioBackend _backend = new();
    private readonly SessionGroupController _controller;
    private readonly VolumeRamp _ramp;
    private readonly WheelAccelerator _accelerator = new();
    private readonly LevelMemory _memory = new();
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _pollTimer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastFrameMs;
    private int _pollCount;
    private bool _disposed;

    public MixerEngine(AppSettings settings)
    {
        _controller = new SessionGroupController(settings.LastLevel);
        _ramp = new VolumeRamp(settings.LastLevel);
        _memory.ReturnLevel = settings.ReturnLevel;

        _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(10),
        };
        _animationTimer.Tick += (_, _) => AdvanceFrame(0);

        _pollTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(PollIntervalMs),
        };
        _pollTimer.Tick += (_, _) => Poll();

        ApplySettings(settings);
    }

    /// <summary>Durum değiştiğinde (seviye, sessiz, bağlantı) tetiklenir.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Şu anda uygulanan (fader'da gösterilen) seviye.</summary>
    public double CurrentLevel => _ramp.Current;

    /// <summary>Gidilmekte olan / kaydedilecek seviye.</summary>
    public double TargetLevel => _ramp.Target;

    public bool IsMuted => _controller.IsMuted;

    public bool IsConnected => _controller.IsConnected;

    public int SessionCount => _controller.SessionCount;

    public double? ReturnLevel => _memory.ReturnLevel;

    public void Start()
    {
        ScanSessions();
        _pollTimer.Start();
    }

    public void ApplySettings(AppSettings settings)
    {
        _accelerator.Enabled = settings.SmartAcceleration;
        _accelerator.BaseStepPercent = settings.WheelStepPercent;
        _accelerator.SlowStepPercent = settings.SlowStepPercent;
        _accelerator.NormalStepPercent = settings.NormalStepPercent;
        _accelerator.FastStepPercent = settings.FastStepPercent;

        _ramp.Enabled = settings.SmoothTransitions;
        _ramp.DurationMs = settings.TransitionMs;
        if (!_ramp.Enabled && _ramp.IsActive)
        {
            _ramp.SetTarget(_ramp.Target);
            WriteLevel();
        }
    }

    /// <summary>Fare tekerleği olayı. Hareketler hedefte birikir.</summary>
    public void OnWheel(int delta, long timestampMs)
    {
        double deltaPercent = _accelerator.ComputeDeltaPercent(delta, timestampMs);
        if (deltaPercent == 0) return;

        // Ses açılmak isteniyorsa sessiz kipten çık.
        if (deltaPercent > 0 && _controller.IsMuted) _controller.SetMuted(false);

        SetLevel(_ramp.Target + (deltaPercent / 100.0), smooth: true);
    }

    /// <summary>Seviyeyi ayarlar. smooth=false ise anında uygulanır (fader sürükleme).</summary>
    public void SetLevel(double level, bool smooth)
    {
        level = VolumeMath.Clamp01(level);

        if (!smooth)
        {
            _animationTimer.Stop();
            _ramp.Reset(level);
            WriteLevel();
            return;
        }

        _ramp.SetTarget(level);
        if (!_ramp.IsActive)
        {
            WriteLevel();
            return;
        }

        if (!_animationTimer.IsEnabled)
        {
            _lastFrameMs = _clock.Elapsed.TotalMilliseconds;
            _animationTimer.Start();
        }

        // İlk adımı bekletmeden hemen uygula: tepki süresi (özellikle ses azaltırken) önceliklidir.
        AdvanceFrame(MinKickFrameMs);
    }

    public void ToggleMute()
    {
        _controller.SetMuted(!_controller.IsMuted);
        RaiseStateChanged();
    }

    public void SetMuted(bool muted)
    {
        _controller.SetMuted(muted);
        RaiseStateChanged();
    }

    /// <summary>LOW / HIGH ön ayarı: ön ayara git, ön ayardayken tekrar basılırsa önceki seviyeye dön.</summary>
    public void TogglePreset(double presetPercent)
    {
        double level = _memory.TogglePreset(_ramp.Target, VolumeMath.FromPercent(presetPercent));
        if (_controller.IsMuted) _controller.SetMuted(false);
        SetLevel(level, smooth: true);
    }

    private void AdvanceFrame(double minimumFrameMs)
    {
        double now = _clock.Elapsed.TotalMilliseconds;
        double elapsed = Math.Max(now - _lastFrameMs, minimumFrameMs);
        _lastFrameMs = now;

        _ramp.Advance(elapsed);
        if (!_ramp.IsActive) _animationTimer.Stop();
        WriteLevel();
    }

    private void WriteLevel()
    {
        _controller.SetMaster(_ramp.Current);
        RaiseStateChanged();
    }

    private void Poll()
    {
        if (_disposed) return;

        _pollCount++;
        if (_pollCount % SessionScanEveryNPolls == 0)
        {
            ScanSessions();
            return;
        }

        CheckExternalChanges();
    }

    private void ScanSessions()
    {
        bool wasConnected = _controller.IsConnected;
        int oldCount = _controller.SessionCount;

        bool adopted;
        try
        {
            adopted = _controller.Sync(_backend.GetChromeSessions());
        }
        catch
        {
            return;
        }

        if (adopted)
        {
            _animationTimer.Stop();
            _ramp.Reset(_controller.Master);
        }

        bool changed = adopted || wasConnected != _controller.IsConnected || oldCount != _controller.SessionCount;
        if (CheckExternalChanges()) changed = false; // zaten bildirildi
        if (changed) RaiseStateChanged();
    }

    /// <returns>Değişiklik bulunup bildirildiyse true.</returns>
    private bool CheckExternalChanges()
    {
        // Geçiş sürerken okunan değer bizim yazdığımızdır; karşılaştırma anlamsız.
        if (_ramp.IsActive || !_controller.IsConnected) return false;

        bool changed;
        try
        {
            changed = _controller.DetectExternalChanges();
        }
        catch
        {
            return false;
        }

        if (!changed) return false;

        _ramp.Reset(_controller.Master);
        RaiseStateChanged();
        return true;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Güvenli kapatma: zamanlayıcılar durur, COM nesneleri bırakılır.
    /// Chrome'un ses seviyesi ve sessiz durumu DEĞİŞTİRİLMEZ.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _animationTimer.Stop();
        _pollTimer.Stop();

        // Yarım kalan bir geçiş varsa hedefe oturt; kaydedilen seviye ile Chrome'daki seviye aynı olsun.
        if (_ramp.IsActive)
        {
            _ramp.Reset(_ramp.Target);
            _controller.SetMaster(_ramp.Current);
        }

        _backend.Dispose();
    }
}
