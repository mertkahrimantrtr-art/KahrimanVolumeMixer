namespace KahrimanVolumeMixer.Core;

/// <summary>
/// Hedefi her an değiştirilebilen, zaman tabanlı yumuşak ses geçişi.
/// Üstel yaklaşım + asgari hız kullanır: yeni hedef geldiğinde geçiş kesilmeden yeni hedefe döner,
/// kuyruk kısmı uzamaz. Ses azaltma, tepki önceliği için daha hızlı yapılır.
/// </summary>
public sealed class VolumeRamp
{
    /// <summary>Bu farkın altında hedefe doğrudan oturulur.</summary>
    public const double SnapEpsilon = 0.0005;

    /// <summary>Tek karede işlenecek en uzun süre; takılmalardan sonra ani sıçramayı sınırlar.</summary>
    public const double MaxFrameMs = 100.0;

    public VolumeRamp(double initial = 0)
    {
        Reset(initial);
    }

    /// <summary>Şu anda uygulanan seviye (0–1).</summary>
    public double Current { get; private set; }

    /// <summary>Ulaşılmak istenen seviye (0–1). Tekerlek hareketleri burada birikir.</summary>
    public double Target { get; private set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Tam bir geçişin yaklaşık süresi (ms).</summary>
    public double DurationMs { get; set; } = 150.0;

    /// <summary>Ses azaltırken geçiş bu katsayı kadar hızlanır (≥1).</summary>
    public double DecreaseSpeedup { get; set; } = 2.0;

    public bool IsActive => Current != Target;

    /// <summary>Geçişi iptal eder ve seviyeyi doğrudan ayarlar.</summary>
    public void Reset(double level)
    {
        Current = Target = VolumeMath.Clamp01(level);
    }

    public void SetTarget(double target)
    {
        Target = VolumeMath.Clamp01(target);
        if (!Enabled || DurationMs <= 1) Current = Target;
    }

    /// <summary>Geçişi verilen süre kadar ilerletir ve yeni seviyeyi döndürür.</summary>
    public double Advance(double elapsedMs)
    {
        if (!IsActive) return Current;
        if (!Enabled || DurationMs <= 1)
        {
            Current = Target;
            return Current;
        }

        double diff = Target - Current;
        if (Math.Abs(diff) <= SnapEpsilon)
        {
            Current = Target;
            return Current;
        }

        if (double.IsNaN(elapsedMs) || elapsedMs <= 0) return Current;
        if (elapsedMs > MaxFrameMs) elapsedMs = MaxFrameMs;

        double duration = DurationMs;
        if (diff < 0 && DecreaseSpeedup > 1) duration /= DecreaseSpeedup;

        double tau = duration / 4.0; // ≈ süre sonunda farkın %98'i kapanır
        double move = diff * (1.0 - Math.Exp(-elapsedMs / tau));

        // Asgari hız: süre başına tam ölçeğin %10'u. Üstel kuyruğun uzamasını önler.
        double minMove = 0.10 * elapsedMs / duration;
        if (Math.Abs(move) < minMove) move = Math.Sign(diff) * minMove;

        if (Math.Abs(move) >= Math.Abs(diff) || Math.Abs(diff - move) <= SnapEpsilon)
        {
            Current = Target;
        }
        else
        {
            Current = VolumeMath.Clamp01(Current + move);
        }

        return Current;
    }
}
