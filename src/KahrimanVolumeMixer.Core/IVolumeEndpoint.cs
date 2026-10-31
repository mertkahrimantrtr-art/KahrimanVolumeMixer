namespace KahrimanVolumeMixer.Core;

/// <summary>
/// Kontrol edilen ses çıkış aygıtı (Windows varsayılan hoparlör/kulaklık ana ses seviyesi).
/// Tüm üyeler istisna fırlatabilir (aygıt çıkarılmış olabilir); çağıran taraf bunu yönetir.
/// </summary>
public interface IVolumeEndpoint
{
    /// <summary>Aygıtın kalıcı, benzersiz kimliği.</summary>
    string Id { get; }

    /// <summary>Ana ses seviyesi (0–1, Windows'ta gösterilen yüzdeyle aynı ölçek).</summary>
    float GetLevel();

    void SetLevel(float level);

    bool GetMute();

    void SetMute(bool mute);
}
