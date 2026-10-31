using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KahrimanVolumeMixer.Controls;

/// <summary>
/// Dikey DJ fader'ı. Doğrudan OnRender ile çizilir (hafif, akıcı).
/// Tıklama/sürükleme ile seviye bildirir; tekerlek olayları pencere düzeyinde işlenir.
/// </summary>
public sealed class FaderControl : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(FaderControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceValue));

    public static readonly DependencyProperty IsMutedProperty = DependencyProperty.Register(
        nameof(IsMuted),
        typeof(bool),
        typeof(FaderControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
        nameof(IsConnected),
        typeof(bool),
        typeof(FaderControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    private const double TrackWidth = 12.0;
    private const double CapHeight = 16.0;
    private const double VerticalPadding = CapHeight / 2.0 + 2.0;

    private bool _dragging;

    public FaderControl()
    {
        Focusable = false;
        Cursor = Cursors.Hand;
        SnapsToDevicePixels = true;
    }

    /// <summary>Kullanıcı tıklayıp sürüklediğinde yeni seviyeyi (0–1) bildirir.</summary>
    public event EventHandler<double>? UserValueChanged;

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsMuted
    {
        get => (bool)GetValue(IsMutedProperty);
        set => SetValue(IsMutedProperty, value);
    }

    public bool IsConnected
    {
        get => (bool)GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        double v = (double)baseValue;
        if (double.IsNaN(v)) return 0.0;
        return Math.Clamp(v, 0.0, 1.0);
    }

    private Rect TrackArea
    {
        get
        {
            double height = Math.Max(0, ActualHeight - (VerticalPadding * 2));
            return new Rect(0, VerticalPadding, ActualWidth, height);
        }
    }

    private double ValueFromPoint(Point p)
    {
        Rect area = TrackArea;
        if (area.Height <= 0) return Value;
        return Math.Clamp(1.0 - ((p.Y - area.Top) / area.Height), 0.0, 1.0);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (CaptureMouse())
        {
            _dragging = true;
            UserValueChanged?.Invoke(this, ValueFromPoint(e.GetPosition(this)));
        }

        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging && IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
        {
            UserValueChanged?.Invoke(this, ValueFromPoint(e.GetPosition(this)));
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragging)
        {
            _dragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _dragging = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        // Tüm alan tıklanabilir/tekerleklenebilir olsun.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        Brush track = GetBrush("TrackBrush", Color.FromRgb(0x2A, 0x2A, 0x30));
        Brush line = GetBrush("LineBrush", Color.FromRgb(0x44, 0x44, 0x4C));
        Brush subText = GetBrush("SubTextBrush", Color.FromRgb(0x8C, 0x8C, 0x96));
        Color accent = GetColor("AccentBrush", Color.FromRgb(0xFF, 0x8A, 0x1F));
        Color dim = GetColor("OffBrush", Color.FromRgb(0x6B, 0x6B, 0x73));
        Brush capBrush = GetBrush("CapBrush", Color.FromRgb(0xE8, 0xE8, 0xEC));

        bool active = IsConnected && !IsMuted;
        Color fillColor = active ? accent : dim;

        Rect area = TrackArea;
        double centerX = Math.Round(width / 2.0);
        double trackLeft = centerX - (TrackWidth / 2.0);
        var trackRect = new Rect(trackLeft, area.Top, TrackWidth, area.Height);

        // Ölçek çizgileri: her %10'da küçük, %0/25/50/75/100'de büyük.
        var linePen = new Pen(line, 1.0);
        linePen.Freeze();
        double tickOuter = Math.Min(16.0, Math.Max(6.0, (width - TrackWidth) / 2.0 - 6.0));
        for (int i = 0; i <= 20; i++)
        {
            double pct = i * 5;
            bool major = i % 5 == 0;
            bool minor = i % 2 == 0;
            if (!major && !minor) continue;

            double y = Math.Round(area.Bottom - (area.Height * pct / 100.0)) + 0.5;
            double len = major ? tickOuter : tickOuter * 0.55;
            dc.DrawLine(linePen, new Point(trackLeft - 4 - len, y), new Point(trackLeft - 4, y));
            dc.DrawLine(linePen, new Point(trackLeft + TrackWidth + 4, y), new Point(trackLeft + TrackWidth + 4 + len, y));
        }

        // Ray
        dc.DrawRoundedRectangle(track, null, trackRect, TrackWidth / 2.0, TrackWidth / 2.0);

        // Dolgu
        double value = Value;
        double fillHeight = area.Height * value;
        if (fillHeight > 0.5)
        {
            var fillRect = new Rect(trackLeft, area.Bottom - fillHeight, TrackWidth, fillHeight);
            var gradient = new LinearGradientBrush(
                Lighten(fillColor, 0.30),
                Darken(fillColor, 0.25),
                new Point(0.5, 0),
                new Point(0.5, 1));
            gradient.Freeze();
            double radius = Math.Min(TrackWidth / 2.0, fillHeight / 2.0);
            dc.DrawRoundedRectangle(gradient, null, fillRect, radius, radius);
        }

        // Kapak (fader düğmesi)
        double capWidth = Math.Min(Math.Max(TrackWidth + 16, width - 18), 48);
        double capY = area.Bottom - fillHeight;
        var capRect = new Rect(centerX - (capWidth / 2.0), capY - (CapHeight / 2.0), capWidth, CapHeight);
        var shadowRect = capRect;
        shadowRect.Offset(0, 1.5);
        var shadow = new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0));
        shadow.Freeze();
        dc.DrawRoundedRectangle(shadow, null, shadowRect, 4, 4);
        dc.DrawRoundedRectangle(capBrush, null, capRect, 4, 4);

        var markPen = new Pen(new SolidColorBrush(fillColor), 2.0);
        markPen.Freeze();
        dc.DrawLine(markPen, new Point(capRect.Left + 5, capY), new Point(capRect.Right - 5, capY));

        if (!IsConnected)
        {
            DrawCenteredText(dc, "—", subText, centerX, area.Top + (area.Height / 2.0));
        }
    }

    private void DrawCenteredText(DrawingContext dc, string text, Brush brush, double cx, double cy)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point(cx - (formatted.Width / 2.0), cy - (formatted.Height / 2.0)));
    }

    private Brush GetBrush(string key, Color fallback) =>
        TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    private Color GetColor(string key, Color fallback) =>
        TryFindResource(key) is SolidColorBrush brush ? brush.Color : fallback;

    private static Color Lighten(Color c, double amount) => Color.FromArgb(
        c.A,
        (byte)Math.Min(255, c.R + ((255 - c.R) * amount)),
        (byte)Math.Min(255, c.G + ((255 - c.G) * amount)),
        (byte)Math.Min(255, c.B + ((255 - c.B) * amount)));

    private static Color Darken(Color c, double amount) => Color.FromArgb(
        c.A,
        (byte)Math.Max(0, c.R * (1 - amount)),
        (byte)Math.Max(0, c.G * (1 - amount)),
        (byte)Math.Max(0, c.B * (1 - amount)));
}
