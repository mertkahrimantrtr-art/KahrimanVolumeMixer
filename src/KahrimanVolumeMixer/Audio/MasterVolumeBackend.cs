using System;
using KahrimanVolumeMixer.Core;
using NAudio.CoreAudioApi;

namespace KahrimanVolumeMixer.Audio;

/// <summary>
/// Windows Core Audio: varsayılan ses çıkış aygıtının (DataFlow.Render, Role.Multimedia)
/// ana ses seviyesi — AudioEndpointVolume.MasterVolumeLevelScalar ve Mute.
/// Windows'taki değişiklikler OnVolumeNotification ile anında bildirilir.
/// Varsayılan aygıt değişimi Refresh() çağrılarında (≈1 sn) algılanır.
/// Tüm çağrılar UI (STA) iş parçacığından yapılmalıdır; VolumeNotification ise herhangi
/// bir iş parçacığında tetiklenebilir.
/// </summary>
internal sealed class MasterVolumeBackend : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;
    private AudioEndpointVolume? _volume;
    private Endpoint? _endpoint;
    private bool _disposed;

    /// <summary>Windows'ta ses değiştiğinde tetiklenir (arka plan iş parçacığından).</summary>
    public event EventHandler? VolumeNotification;

    /// <summary>Bağlı aygıtın adı (ör. "Hoparlör (Realtek)").</summary>
    public string? DeviceName { get; private set; }

    /// <summary>
    /// Varsayılan aygıtı kontrol eder. Aygıt değiştiyse eskisini bırakıp yenisine bağlanır.
    /// Asla istisna fırlatmaz.
    /// </summary>
    /// <param name="changed">Bağlı aygıt değiştiyse (veya bağlantı koptuysa) true.</param>
    public IVolumeEndpoint? Refresh(out bool changed)
    {
        changed = false;
        if (_disposed) return null;

        MMDevice? device = null;
        string? id = null;
        try
        {
            device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            id = device.ID;
        }
        catch
        {
            // Hiç ses çıkış aygıtı yok veya erişilemiyor.
            SafeDispose(device);
            device = null;
            id = null;
        }

        if (string.Equals(id, _endpoint?.Id, StringComparison.Ordinal))
        {
            SafeDispose(device);
            return _endpoint;
        }

        Release();
        changed = true;
        if (device is null || id is null) return null;

        try
        {
            AudioEndpointVolume volume = device.AudioEndpointVolume;
            volume.OnVolumeNotification += OnVolumeNotification;
            _device = device;
            _volume = volume;
            _endpoint = new Endpoint(id, volume);
            DeviceName = TryGetName(device);
            return _endpoint;
        }
        catch
        {
            Release();
            SafeDispose(device);
            return null;
        }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        VolumeNotification?.Invoke(this, EventArgs.Empty);
    }

    private static string? TryGetName(MMDevice device)
    {
        try
        {
            return device.FriendlyName;
        }
        catch
        {
            return null;
        }
    }

    private void Release()
    {
        if (_volume is not null)
        {
            try
            {
                _volume.OnVolumeNotification -= OnVolumeNotification;
            }
            catch
            {
                // Aygıt zaten geçersiz olabilir.
            }
        }

        SafeDispose(_volume);
        SafeDispose(_device);
        _volume = null;
        _device = null;
        _endpoint = null;
        DeviceName = null;
    }

    private static void SafeDispose(object? instance)
    {
        try
        {
            if (instance is IDisposable disposable) disposable.Dispose();
        }
        catch
        {
            // COM nesnesi zaten geçersiz olabilir.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Release();
        SafeDispose(_enumerator);
    }

    /// <summary>Varsayılan aygıtın ana ses seviyesi.</summary>
    private sealed class Endpoint : IVolumeEndpoint
    {
        private readonly AudioEndpointVolume _volume;

        public Endpoint(string id, AudioEndpointVolume volume)
        {
            Id = id;
            _volume = volume;
        }

        public string Id { get; }

        public float GetLevel() => _volume.MasterVolumeLevelScalar;

        public void SetLevel(float level) => _volume.MasterVolumeLevelScalar = Math.Clamp(level, 0f, 1f);

        public bool GetMute() => _volume.Mute;

        public void SetMute(bool mute)
        {
            if (_volume.Mute != mute) _volume.Mute = mute;
        }
    }
}
