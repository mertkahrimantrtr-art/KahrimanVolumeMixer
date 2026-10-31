namespace KahrimanVolumeMixer.Core;

/// <summary>
/// Windows ana ses seviyesi ile mikser arasındaki iki yönlü senkronizasyon.
///
/// Kurallar:
/// • Bir aygıta bağlanıldığında (açılış veya varsayılan aygıt değişimi) aygıtın MEVCUT seviyesi ve
///   sessiz durumu benimsenir. Kayıtlı eski seviye Windows'a asla otomatik uygulanmaz.
/// • Mikserden yapılan değişiklik doğrudan aygıta yazılır.
/// • Aygıttaki değer, en son bizim yazdığımız değerden farklıysa dış değişiklik (Windows ses
///   simgesi, klavye ses tuşları vb.) sayılır ve mikser bu değeri benimser.
/// • Aygıta erişim hata verirse bağlantı düşürülür; uygulama çökmez.
/// </summary>
public sealed class MasterVolumeController
{
    /// <summary>Kendi yazdığımız değerden bu kadar sapma "dış değişiklik" sayılır.</summary>
    public const double ExternalChangeTolerance = 0.004;

    private IVolumeEndpoint? _endpoint;
    private double _lastWritten = -1;
    private bool _lastMute;

    public MasterVolumeController(double initialLevel = 0)
    {
        Level = VolumeMath.Clamp01(initialLevel);
    }

    /// <summary>Ana ses seviyesi (0–1).</summary>
    public double Level { get; private set; }

    public bool IsMuted { get; private set; }

    public bool IsConnected => _endpoint is not null;

    public string? EndpointId => _endpoint?.Id;

    /// <summary>Yeni aygıta bağlanır (null: bağlantıyı kes) ve aygıtın mevcut durumunu benimser.</summary>
    /// <returns>Aygıtın seviyesi benimsendiyse true.</returns>
    public bool Attach(IVolumeEndpoint? endpoint)
    {
        _endpoint = endpoint;
        _lastWritten = -1;
        if (endpoint is null) return false;

        try
        {
            double level = VolumeMath.Clamp01(endpoint.GetLevel());
            bool muted = endpoint.GetMute();
            Level = level;
            IsMuted = muted;
            _lastWritten = level;
            _lastMute = muted;
            return true;
        }
        catch
        {
            _endpoint = null;
            return false;
        }
    }

    /// <summary>Ses seviyesini ayarlar ve aygıta yazar.</summary>
    public void SetLevel(double level)
    {
        Level = VolumeMath.Clamp01(level);
        if (_endpoint is null) return;

        try
        {
            _endpoint.SetLevel((float)Level);
            _lastWritten = Level;
        }
        catch
        {
            _endpoint = null;
        }
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        if (_endpoint is null) return;

        try
        {
            _endpoint.SetMute(muted);
            _lastMute = muted;
        }
        catch
        {
            _endpoint = null;
        }
    }

    /// <summary>Aygıtı okuyup dışarıdan yapılan değişiklikleri benimser.</summary>
    /// <returns>Seviye, sessiz durumu veya bağlantı değiştiyse true.</returns>
    public bool DetectExternalChanges()
    {
        if (_endpoint is null) return false;

        double level;
        bool muted;
        try
        {
            level = VolumeMath.Clamp01(_endpoint.GetLevel());
            muted = _endpoint.GetMute();
        }
        catch
        {
            _endpoint = null;
            return true;
        }

        bool levelChanged = _lastWritten < 0 || Math.Abs(level - _lastWritten) > ExternalChangeTolerance;
        bool muteChanged = muted != _lastMute;
        if (!levelChanged && !muteChanged) return false;

        Level = level;
        IsMuted = muted;
        _lastWritten = level;
        _lastMute = muted;
        return true;
    }
}
