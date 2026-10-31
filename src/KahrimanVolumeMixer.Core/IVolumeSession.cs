namespace KahrimanVolumeMixer.Core;

/// <summary>
/// Kontrol edilen tek bir ses oturumu (ör. Chrome'un bir WASAPI oturumu).
/// Tüm üyeler istisna fırlatabilir (oturum kapanmış olabilir); çağıran taraf bunu yönetir.
/// </summary>
public interface IVolumeSession
{
    /// <summary>Oturumun kalıcı, benzersiz anahtarı.</summary>
    string Key { get; }

    float GetVolume();

    void SetVolume(float volume);

    bool GetMute();

    void SetMute(bool mute);
}
