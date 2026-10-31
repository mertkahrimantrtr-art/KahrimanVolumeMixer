using System;
using System.Diagnostics;
using System.Windows.Threading;
using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Audio;

/// <summary>
/// Uygulamanın ses motoru: tekerlek → hızlandırma → hedef birikimi → yumuşak geçiş → Windows ana ses.
/// Tüm işlemler UI iş parçacığında, düşük maliyetli zamanlayıcılarla çalışır:
///  • Geçiş zamanlayıcısı yalnızca bir geçiş sürerken çalışır (≈60 Hz).
///  • Windows'taki ses değişiklikleri OnVolumeNotification ile anında yakalanır.
///  • Yoklama zamanlayıcısı yedek olarak 250 ms'de bir değeri, ≈1 sn'de bir varsayılan aygıtı kontrol eder.
/// </summary>
internal sealed class MixerEngine : IDisposable
{
    private const int PollIntervalMs = 250;
    private const int DeviceCheckEveryNPolls = 4;
    private const double MinKickFrameMs = 8.0;

    private readonly MasterVolumeBackend _backend = new();
    private readonly MasterVolumeController _controller;
    private readonly VolumeRamp _ramp;
    private readonly WheelAccelerator _accelerator = new();
    private readonly LevelMemory _memory = new();
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _pollTimer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastFrameMs;
    private int _pollCount;
    private int _notificationQueued;
    private bool _disposed;

    public MixerEngine(AppSettings settings)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;

        // Kayıtlı seviye yalnızca aygıt bulunana kadar gösterilir; Windows'a uygulanmaz.
        _controller = new MasterVolumeController(settings.LastLevel);
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

        _backend.VolumeNotification += OnBackendVolumeNotification;

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

    public string? DeviceName => _backend.DeviceName;

    public double? ReturnLevel => _memory.ReturnLevel;

    /// <summary>Varsayılan aygıta bağlanır ve Windows'un mevcut ses seviyesiyle başlar.</summary>
    public void Start()
    {
        RefreshDevice();
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
        _controller.SetLevel(_ramp.Current);
        RaiseStateChanged();
    }

    /// <summary>Windows'tan gelen bildirim (arka plan iş parçacığı) → UI iş parçacığında kontrol.</summary>
    private void OnBackendVolumeNotification(object? sender, EventArgs e)
    {
        if (_disposed) return;

        // Hızlı ardışık bildirimleri tek bir kontrole indir.
        if (System.Threading.Interlocked.Exchange(ref _notificationQueued, 1) == 1) return;

        _dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            System.Threading.Interlocked.Exchange(ref _notificationQueued, 0);
            if (!_disposed) CheckExternalChanges();
        }));
    }

    private void Poll()
    {
        if (_disposed) return;

        _pollCount++;
        if (_pollCount % DeviceCheckEveryNPolls == 0)
        {
            RefreshDevice();
            return;
        }

        CheckExternalChanges();
    }

    private void RefreshDevice()
    {
        IVolumeEndpoint? endpoint = _backend.Refresh(out bool changed);

        if (!changed && endpoint is not null && !_controller.IsConnected)
        {
            // Aygıt aynı ama önceki bir hata bağlantıyı düşürmüş: yeniden bağlan.
            changed = true;
        }

        if (changed)
        {
            _animationTimer.Stop();
            if (_controller.Attach(endpoint))
            {
                _ramp.Reset(_controller.Level);
            }
            else
            {
                _ramp.Reset(_ramp.Target);
            }

            RaiseStateChanged();
            return;
        }

        CheckExternalChanges();
    }

    /// <returns>Değişiklik bulunup bildirildiyse true.</returns>
    private bool CheckExternalChanges()
    {
        // Geçiş sürerken okunan değer bizim yazdığımızdır; karşılaştırma anlamsız.
        if (_ramp.IsActive || !_controller.IsConnected) return false;

        if (!_controller.DetectExternalChanges()) return false;

        _ramp.Reset(_controller.Level);
        RaiseStateChanged();
        return true;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Güvenli kapatma: zamanlayıcılar durur, olay abonelikleri ve COM nesneleri bırakılır.
    /// Windows ses seviyesi ve sessiz durumu sıfırlanmaz.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _animationTimer.Stop();
        _pollTimer.Stop();

        // Yarım kalan bir geçiş varsa hedefe oturt; kaydedilen seviye ile Windows'taki seviye aynı olsun.
        if (_ramp.IsActive)
        {
            _ramp.Reset(_ramp.Target);
            _controller.SetLevel(_ramp.Current);
        }

        _backend.VolumeNotification -= OnBackendVolumeNotification;
        _backend.Dispose();
    }
}
