namespace KahrimanVolumeMixer.Core;

/// <summary>Ses seviyesi hesaplarında kullanılan yardımcılar. Seviyeler içeride 0.0–1.0 aralığında tutulur.</summary>
public static class VolumeMath
{
    public static double Clamp01(double value)
    {
        if (double.IsNaN(value)) return 0;
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }

    public static double Clamp(double value, double min, double max, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return fallback;
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static int ToPercent(double level) =>
        (int)Math.Round(Clamp01(level) * 100.0, MidpointRounding.AwayFromZero);

    public static double FromPercent(double percent) => Clamp01(percent / 100.0);
}
