namespace KahrimanVolumeMixer.Core;

/// <summary>
/// LOW / HIGH butonlarının "seviyeye git — tekrar basınca geri dön" hafızası.
/// Bir ön ayara geçildiğinde terk edilen seviye saklanır. Ön ayardayken aynı butona
/// basılırsa saklanan seviyeye dönülür ve terk edilen ön ayar saklanır.
/// Örnek: %75 → LOW → %10 → LOW → %75 → LOW → %10 ...
///        %10 → HIGH → %75 → HIGH → %10 (LOW ↔ HIGH geçişi)
/// </summary>
public sealed class LevelMemory
{
    /// <summary>Bu farkın içindeki seviye "ön ayarda" sayılır (±1.5 puan).</summary>
    public const double Tolerance = 0.015;

    /// <summary>Geri dönülecek seviye (0–1).</summary>
    public double? ReturnLevel { get; set; }

    /// <returns>Gidilecek yeni seviye (0–1).</returns>
    public double TogglePreset(double current, double preset)
    {
        current = VolumeMath.Clamp01(current);
        preset = VolumeMath.Clamp01(preset);

        bool atPreset = Math.Abs(current - preset) <= Tolerance;
        if (atPreset)
        {
            if (ReturnLevel is double back && Math.Abs(back - preset) > Tolerance)
            {
                ReturnLevel = current;
                return back;
            }

            return preset;
        }

        ReturnLevel = current;
        return preset;
    }
}
