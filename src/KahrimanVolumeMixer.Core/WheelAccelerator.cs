namespace KahrimanVolumeMixer.Core;

/// <summary>
/// Fare tekerleği olaylarını ses değişimine (yüzde puanı) çevirir.
/// Akıllı hızlandırma açıkken tekerleğin dönüş hızı (çentik/saniye) ölçülür ve
/// yavaş / normal / hızlı adım değerlerinden biri uygulanır.
/// Yüksek çözünürlüklü tekerlek ve dokunmatik yüzeylerin kesirli delta değerleri orantılı işlenir.
/// </summary>
public sealed class WheelAccelerator
{
    /// <summary>Windows'ta bir tekerlek çentiğinin delta değeri.</summary>
    public const double NotchDelta = 120.0;

    /// <summary>Anlık hız ölçümünde üst sınır (çentik/sn). Toplu gelen olayların hızı uçurmasını engeller.</summary>
    private const double MaxInstantRate = 120.0;

    /// <summary>İki olay arasında kabul edilen en kısa süre (ms).</summary>
    private const double MinIntervalMs = 4.0;

    public bool Enabled { get; set; } = true;

    /// <summary>Hızlandırma kapalıyken çentik başına sabit adım (%).</summary>
    public double BaseStepPercent { get; set; } = 2.0;

    public double SlowStepPercent { get; set; } = 1.5;
    public double NormalStepPercent { get; set; } = 5.0;
    public double FastStepPercent { get; set; } = 12.0;

    /// <summary>Bu hızın (çentik/sn) üzerindeki hareketler "normal" sayılır.</summary>
    public double NormalRateThreshold { get; set; } = 7.0;

    /// <summary>Bu hızın (çentik/sn) üzerindeki hareketler "hızlı" sayılır.</summary>
    public double FastRateThreshold { get; set; } = 18.0;

    /// <summary>Bu süreden (ms) uzun bir ara yeni bir hareket başlatır ve hız sıfırlanır.</summary>
    public double BurstGapMs { get; set; } = 250.0;

    private double _rate;
    private long _lastTimestamp;
    private bool _hasLast;
    private int _lastDirection;

    /// <summary>Ölçülen yumuşatılmış dönüş hızı (çentik/sn).</summary>
    public double CurrentRate => _rate;

    public void Reset()
    {
        _rate = 0;
        _hasLast = false;
        _lastDirection = 0;
    }

    /// <summary>
    /// Bir tekerlek olayını yüzde puanı cinsinden ses değişimine çevirir.
    /// Pozitif sonuç sesi artırır, negatif sonuç azaltır.
    /// </summary>
    /// <param name="wheelDelta">Olayın delta değeri (bir çentik = ±120).</param>
    /// <param name="timestampMs">Olay zamanı (ms, monoton; Environment.TickCount taşması tolere edilir).</param>
    public double ComputeDeltaPercent(int wheelDelta, long timestampMs)
    {
        if (wheelDelta == 0) return 0;

        double notches = wheelDelta / NotchDelta;
        int direction = Math.Sign(wheelDelta);

        double elapsed = _hasLast ? timestampMs - _lastTimestamp : double.PositiveInfinity;
        bool newBurst = !_hasLast || elapsed < 0 || elapsed > BurstGapMs || direction != _lastDirection;

        if (newBurst)
        {
            _rate = 0;
        }
        else
        {
            double instant = Math.Abs(notches) * 1000.0 / Math.Max(elapsed, MinIntervalMs);
            if (instant > MaxInstantRate) instant = MaxInstantRate;
            _rate = _rate <= 0 ? instant : (_rate * 0.4) + (instant * 0.6);
        }

        _lastTimestamp = timestampMs;
        _hasLast = true;
        _lastDirection = direction;

        if (!Enabled) return notches * BaseStepPercent;
        return notches * StepForRate(_rate);
    }

    public double StepForRate(double rate)
    {
        if (rate >= FastRateThreshold) return FastStepPercent;
        if (rate >= NormalRateThreshold) return NormalStepPercent;
        return SlowStepPercent;
    }
}
