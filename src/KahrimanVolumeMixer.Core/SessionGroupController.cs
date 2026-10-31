namespace KahrimanVolumeMixer.Core;

/// <summary>
/// Chrome'un tüm ses oturumlarını tek bir "ana seviye" ile birlikte yönetir.
///
/// VARSAYILAN DAVRANIŞ (göreli dengeyi koruma):
/// • Ana seviye = oturumların en yükseğinin seviyesi.
/// • Her oturumun oranı = kendi seviyesi / ana seviye. Ana seviye değişince her oturum oran × ana seviye olur.
///   Böylece oturumlar arasındaki göreli denge korunur; en yüksek oturum fader'ı birebir izler.
/// • Ana seviye 0'a indiğinde oranlar saklanır; ses geri açıldığında denge aynen döner.
/// • Oranlar yalnızca dışarıdan (Windows ses mikseri vb.) bir değişiklik algılandığında yeniden ölçülür.
/// • Chrome zaten açıkken gelen yeni oturumlar oran 1.0 ile ana seviyeye ve sessiz durumuna eşitlenir.
/// • Chrome bağlı değilken bağlanırsa Chrome'un mevcut seviyesi benimsenir. Bağlı değilken kullanıcı
///   fader'ı oynattıysa, bağlantı kurulduğunda kullanıcının seviyesi uygulanır.
/// • Sessize alma Windows'un yerel mute özelliğiyle yapılır; seviye korunur.
/// </summary>
public sealed class SessionGroupController
{
    /// <summary>Kendi yazdığımız değerden bu kadar sapma "dış değişiklik" sayılır.</summary>
    public const double ExternalChangeTolerance = 0.006;

    private const double ZeroThreshold = 0.0001;

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private bool _pendingUserIntent;

    public SessionGroupController(double initialMaster, bool initialMuted = false)
    {
        Master = VolumeMath.Clamp01(initialMaster);
        IsMuted = initialMuted;
    }

    /// <summary>Ana seviye (0–1).</summary>
    public double Master { get; private set; }

    public bool IsMuted { get; private set; }

    public int SessionCount => _entries.Count;

    public bool IsConnected => _entries.Count > 0;

    public bool HasPendingUserIntent => _pendingUserIntent;

    public IReadOnlyDictionary<string, double> GetRatios() =>
        _entries.ToDictionary(pair => pair.Key, pair => pair.Value.Ratio, StringComparer.Ordinal);

    /// <summary>
    /// Mevcut oturum listesini uygular. Kaybolan oturumlar bırakılır, yeni oturumlar eşitlenir.
    /// </summary>
    /// <returns>Ana seviye / sessiz durumu Chrome'dan benimsendiyse true (arayüz senkronlanmalı).</returns>
    public bool Sync(IEnumerable<IVolumeSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        bool wasConnected = _entries.Count > 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var added = new List<Entry>();

        foreach (var session in sessions)
        {
            if (session is null || !seen.Add(session.Key)) continue;

            if (_entries.TryGetValue(session.Key, out var existing))
            {
                existing.Session = session;
                continue;
            }

            var entry = new Entry(session);
            _entries[session.Key] = entry;
            added.Add(entry);
        }

        if (_entries.Count != seen.Count)
        {
            var stale = _entries.Keys.Where(key => !seen.Contains(key)).ToList();
            foreach (var key in stale) _entries.Remove(key);
        }

        if (added.Count == 0) return false;

        if (!wasConnected && !_pendingUserIntent)
        {
            return Adopt(added);
        }

        _pendingUserIntent = false;
        foreach (var entry in added)
        {
            TryWriteVolume(entry, entry.Ratio * Master);
            TryWriteMute(entry, IsMuted);
        }

        RemoveFaulted();
        return false;
    }

    /// <summary>Ana seviyeyi ayarlar ve tüm oturumlara oranlarıyla uygular.</summary>
    public void SetMaster(double level, bool userInitiated = true)
    {
        Master = VolumeMath.Clamp01(level);

        if (_entries.Count == 0)
        {
            if (userInitiated) _pendingUserIntent = true;
            return;
        }

        foreach (var entry in _entries.Values)
        {
            TryWriteVolume(entry, entry.Ratio * Master);
        }

        RemoveFaulted();
    }

    public void SetMuted(bool muted, bool userInitiated = true)
    {
        IsMuted = muted;

        if (_entries.Count == 0)
        {
            if (userInitiated) _pendingUserIntent = true;
            return;
        }

        foreach (var entry in _entries.Values)
        {
            TryWriteMute(entry, muted);
        }

        RemoveFaulted();
    }

    /// <summary>
    /// Oturumları okuyup başka bir yerden (Windows ses mikseri vb.) yapılan değişiklikleri algılar.
    /// </summary>
    /// <returns>Ana seviye, denge veya sessiz durumu değiştiyse true.</returns>
    public bool DetectExternalChanges()
    {
        if (_entries.Count == 0) return false;

        var readings = new List<Reading>(_entries.Count);
        bool volumeChanged = false;
        bool muteChanged = false;

        foreach (var entry in _entries.Values)
        {
            double volume;
            bool muted;
            try
            {
                volume = VolumeMath.Clamp01(entry.Session.GetVolume());
                muted = entry.Session.GetMute();
            }
            catch
            {
                entry.Faulted = true;
                continue;
            }

            readings.Add(new Reading(entry, volume, muted));
            if (entry.LastWritten < 0 || Math.Abs(volume - entry.LastWritten) > ExternalChangeTolerance) volumeChanged = true;
            if (muted != entry.LastMute) muteChanged = true;
        }

        RemoveFaulted();
        if (readings.Count == 0) return false;

        if (volumeChanged)
        {
            double max = readings.Max(r => r.Volume);
            foreach (var r in readings)
            {
                if (max > ZeroThreshold) r.Entry.Ratio = r.Volume / max;
                r.Entry.LastWritten = r.Volume;
            }

            Master = max;
        }

        if (muteChanged)
        {
            foreach (var r in readings) r.Entry.LastMute = r.Muted;
            IsMuted = readings.All(r => r.Muted);
        }

        return volumeChanged || muteChanged;
    }

    private bool Adopt(List<Entry> added)
    {
        var readings = new List<Reading>(added.Count);
        foreach (var entry in added)
        {
            try
            {
                readings.Add(new Reading(entry, VolumeMath.Clamp01(entry.Session.GetVolume()), entry.Session.GetMute()));
            }
            catch
            {
                _entries.Remove(entry.Session.Key);
            }
        }

        if (readings.Count == 0) return false;

        double max = readings.Max(r => r.Volume);
        foreach (var r in readings)
        {
            r.Entry.Ratio = max > ZeroThreshold ? r.Volume / max : 1.0;
            r.Entry.LastWritten = r.Volume;
            r.Entry.LastMute = r.Muted;
        }

        Master = max;
        IsMuted = readings.All(r => r.Muted);
        return true;
    }

    private static void TryWriteVolume(Entry entry, double volume)
    {
        volume = VolumeMath.Clamp01(volume);
        try
        {
            entry.Session.SetVolume((float)volume);
            entry.LastWritten = volume;
        }
        catch
        {
            entry.Faulted = true;
        }
    }

    private static void TryWriteMute(Entry entry, bool muted)
    {
        try
        {
            entry.Session.SetMute(muted);
            entry.LastMute = muted;
        }
        catch
        {
            entry.Faulted = true;
        }
    }

    private void RemoveFaulted()
    {
        List<string>? faulted = null;
        foreach (var pair in _entries)
        {
            if (pair.Value.Faulted) (faulted ??= new List<string>()).Add(pair.Key);
        }

        if (faulted is null) return;
        foreach (var key in faulted) _entries.Remove(key);
    }

    private sealed class Entry
    {
        public Entry(IVolumeSession session)
        {
            Session = session;
        }

        public IVolumeSession Session { get; set; }
        public double Ratio { get; set; } = 1.0;
        public double LastWritten { get; set; } = -1.0;
        public bool LastMute { get; set; }
        public bool Faulted { get; set; }
    }

    private readonly record struct Reading(Entry Entry, double Volume, bool Muted);
}
