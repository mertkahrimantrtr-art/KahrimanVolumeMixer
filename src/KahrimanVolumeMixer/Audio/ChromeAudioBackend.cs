using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KahrimanVolumeMixer.Core;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace KahrimanVolumeMixer.Audio;

/// <summary>
/// Windows Core Audio (WASAPI) üzerinden Google Chrome'un ses oturumlarını bulur.
/// Tüm etkin çıkış aygıtları taranır (Chrome farklı bir hoparlör/kulaklığa çalıyor olabilir).
/// Yalnızca chrome.exe süreçlerine ait oturumlar döndürülür; Windows genel sesine ve diğer
/// uygulamalara dokunulmaz. Tüm çağrılar UI (STA) iş parçacığından yapılmalıdır.
/// </summary>
internal sealed class ChromeAudioBackend : IDisposable
{
    private const string ChromeProcessName = "chrome";

    /// <summary>Aygıt listesi her N oturum taramasında bir yenilenir.</summary>
    private const int DeviceRefreshInterval = 5;

    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly Dictionary<string, DeviceEntry> _devices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChromeSession> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, bool> _chromePidCache = new();
    private int _scanCount;
    private bool _disposed;

    /// <summary>Şu anda var olan (süresi dolmamış) Chrome ses oturumlarını döndürür. Asla istisna fırlatmaz.</summary>
    public IReadOnlyList<IVolumeSession> GetChromeSessions()
    {
        if (_disposed) return Array.Empty<IVolumeSession>();

        if (_devices.Count == 0 || _scanCount % DeviceRefreshInterval == 0)
        {
            RefreshDevices();
        }

        _scanCount++;

        var result = new List<IVolumeSession>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var seenPids = new HashSet<uint>();

        foreach (var pair in _devices.ToList())
        {
            try
            {
                ScanDevice(pair.Key, pair.Value, result, seenKeys, seenPids);
            }
            catch
            {
                // Aygıt çıkarılmış veya geçersiz olabilir; bir sonraki aygıt taramasında yeniden denenir.
                RemoveDevice(pair.Key);
            }
        }

        foreach (var key in _sessions.Keys.Where(k => !seenKeys.Contains(k)).ToList())
        {
            _sessions[key].Dispose();
            _sessions.Remove(key);
        }

        foreach (var pid in _chromePidCache.Keys.Where(p => !seenPids.Contains(p)).ToList())
        {
            _chromePidCache.Remove(pid);
        }

        return result;
    }

    private void ScanDevice(
        string deviceId,
        DeviceEntry device,
        List<IVolumeSession> result,
        HashSet<string> seenKeys,
        HashSet<uint> seenPids)
    {
        AudioSessionManager manager = device.Manager ??= device.Device.AudioSessionManager;
        manager.RefreshSessions();
        SessionCollection collection = manager.Sessions;
        int count = collection.Count;

        for (int i = 0; i < count; i++)
        {
            AudioSessionControl control = collection[i];
            bool keepControl = false;
            try
            {
                if (control.IsSystemSoundsSession) continue;
                if (control.State == AudioSessionState.AudioSessionStateExpired) continue;

                uint pid = control.GetProcessID;
                seenPids.Add(pid);
                if (!IsChromeProcess(pid)) continue;

                string key = deviceId + "|" + control.GetSessionInstanceIdentifier;
                if (!seenKeys.Add(key)) continue;

                if (_sessions.TryGetValue(key, out var existing))
                {
                    result.Add(existing);
                }
                else
                {
                    var session = new ChromeSession(key, control);
                    _sessions[key] = session;
                    result.Add(session);
                    keepControl = true;
                }
            }
            catch
            {
                // Tek bir oturumun hatası taramayı durdurmaz.
            }
            finally
            {
                if (!keepControl) SafeDispose(control);
            }
        }
    }

    private bool IsChromeProcess(uint pid)
    {
        if (pid == 0) return false;
        if (_chromePidCache.TryGetValue(pid, out bool cached)) return cached;

        bool isChrome = false;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            isChrome = string.Equals(process.ProcessName, ChromeProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            isChrome = false;
        }

        _chromePidCache[pid] = isChrome;
        return isChrome;
    }

    private void RefreshDevices()
    {
        var current = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (MMDevice device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                string id;
                try
                {
                    id = device.ID;
                }
                catch
                {
                    SafeDispose(device);
                    continue;
                }

                if (!current.Add(id) || _devices.ContainsKey(id))
                {
                    SafeDispose(device);
                    continue;
                }

                _devices[id] = new DeviceEntry(device);
            }
        }
        catch
        {
            // Aygıt listesi alınamadıysa mevcut aygıtlarla devam edilir.
            return;
        }

        foreach (var id in _devices.Keys.Where(k => !current.Contains(k)).ToList())
        {
            RemoveDevice(id);
        }
    }

    private void RemoveDevice(string id)
    {
        if (!_devices.TryGetValue(id, out var entry)) return;
        _devices.Remove(id);

        foreach (var key in _sessions.Keys.Where(k => k.StartsWith(id + "|", StringComparison.Ordinal)).ToList())
        {
            _sessions[key].Dispose();
            _sessions.Remove(key);
        }

        entry.Dispose();
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

        foreach (var session in _sessions.Values) session.Dispose();
        _sessions.Clear();

        foreach (var device in _devices.Values) device.Dispose();
        _devices.Clear();

        SafeDispose(_enumerator);
    }

    private sealed class DeviceEntry : IDisposable
    {
        public DeviceEntry(MMDevice device)
        {
            Device = device;
        }

        public MMDevice Device { get; }

        public AudioSessionManager? Manager { get; set; }

        public void Dispose()
        {
            SafeDispose(Manager);
            SafeDispose(Device);
        }
    }

    /// <summary>Tek bir Chrome WASAPI oturumu.</summary>
    private sealed class ChromeSession : IVolumeSession, IDisposable
    {
        private readonly AudioSessionControl _control;
        private readonly SimpleAudioVolume _volume;

        public ChromeSession(string key, AudioSessionControl control)
        {
            Key = key;
            _control = control;
            _volume = control.SimpleAudioVolume;
        }

        public string Key { get; }

        public float GetVolume() => _volume.Volume;

        public void SetVolume(float volume) => _volume.Volume = Math.Clamp(volume, 0f, 1f);

        public bool GetMute() => _volume.Mute;

        public void SetMute(bool mute)
        {
            if (_volume.Mute != mute) _volume.Mute = mute;
        }

        public void Dispose() => SafeDispose(_control);
    }
}
