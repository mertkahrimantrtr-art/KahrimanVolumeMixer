using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer;

/// <summary>Ayarlar penceresi. Değişiklikler anında uygulanır ve otomatik kaydedilir.</summary>
public partial class SettingsWindow : Window
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private AppSettings _working;
    private bool _loading;

    public SettingsWindow(AppSettings current, string settingsPath)
    {
        InitializeComponent();
        _working = current.Clone();
        PathText.Text = "Ayar dosyası: " + settingsPath;

        Load();
        HookEvents();
    }

    /// <summary>Her değişiklikte güncel ayarların bir kopyasıyla tetiklenir.</summary>
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>Ayarlar başka bir yerden (tepsi menüsü, sağ tık) değiştiğinde formu günceller.</summary>
    public void Reload(AppSettings current)
    {
        _working = current.Clone();
        Load();
    }

    private void HookEvents()
    {
        foreach (var slider in new[]
                 {
                     WheelStepSlider, SlowStepSlider, NormalStepSlider, FastStepSlider,
                     TransitionSlider, LowSlider, HighSlider, OpacitySlider,
                 })
        {
            slider.ValueChanged += (_, _) => OnChanged();
        }

        foreach (var check in new[] { SmartAccelerationCheck, SmoothCheck, TopmostCheck, StartupCheck })
        {
            check.Checked += (_, _) => OnChanged();
            check.Unchecked += (_, _) => OnChanged();
        }

        ThemeCombo.SelectionChanged += (_, _) => OnChanged();
    }

    private void Load()
    {
        _loading = true;
        try
        {
            SmartAccelerationCheck.IsChecked = _working.SmartAcceleration;
            WheelStepSlider.Value = _working.WheelStepPercent;
            SlowStepSlider.Value = _working.SlowStepPercent;
            NormalStepSlider.Value = _working.NormalStepPercent;
            FastStepSlider.Value = _working.FastStepPercent;

            SmoothCheck.IsChecked = _working.SmoothTransitions;
            TransitionSlider.Value = _working.TransitionMs;

            LowSlider.Value = _working.LowLevelPercent;
            HighSlider.Value = _working.HighLevelPercent;

            TopmostCheck.IsChecked = _working.AlwaysOnTop;
            StartupCheck.IsChecked = _working.StartWithWindows;
            ThemeCombo.SelectedIndex = _working.Theme == AppTheme.Light ? 1 : 0;
            OpacitySlider.Value = Math.Round(_working.WindowOpacity * 100);
        }
        finally
        {
            _loading = false;
        }

        UpdateLabels();
    }

    private void OnChanged()
    {
        if (_loading) return;

        _working.SmartAcceleration = SmartAccelerationCheck.IsChecked == true;
        _working.WheelStepPercent = WheelStepSlider.Value;
        _working.SlowStepPercent = SlowStepSlider.Value;
        _working.NormalStepPercent = NormalStepSlider.Value;
        _working.FastStepPercent = FastStepSlider.Value;
        _working.SmoothTransitions = SmoothCheck.IsChecked == true;
        _working.TransitionMs = TransitionSlider.Value;
        _working.LowLevelPercent = LowSlider.Value;
        _working.HighLevelPercent = HighSlider.Value;
        _working.AlwaysOnTop = TopmostCheck.IsChecked == true;
        _working.StartWithWindows = StartupCheck.IsChecked == true;
        _working.Theme = ThemeCombo.SelectedIndex == 1 ? AppTheme.Light : AppTheme.Dark;
        _working.WindowOpacity = OpacitySlider.Value / 100.0;
        _working.Normalize();

        UpdateLabels();
        SettingsChanged?.Invoke(this, _working.Clone());
    }

    private void UpdateLabels()
    {
        bool smart = SmartAccelerationCheck.IsChecked == true;

        WheelStepLabel.Text = string.Format(Tr, "Sabit adım (hızlandırma kapalı): %{0:0.#}", WheelStepSlider.Value);
        SlowStepLabel.Text = string.Format(Tr, "Yavaş hareket adımı: %{0:0.#}", SlowStepSlider.Value);
        NormalStepLabel.Text = string.Format(Tr, "Normal hareket adımı: %{0:0.#}", NormalStepSlider.Value);
        FastStepLabel.Text = string.Format(Tr, "Hızlı hareket adımı: %{0:0.#}", FastStepSlider.Value);
        TransitionLabel.Text = string.Format(Tr, "Geçiş süresi: {0:0} ms", TransitionSlider.Value);
        LowLabel.Text = string.Format(Tr, "LOW seviyesi: %{0:0}", LowSlider.Value);
        HighLabel.Text = string.Format(Tr, "HIGH seviyesi: %{0:0}", HighSlider.Value);
        OpacityLabel.Text = string.Format(Tr, "Pencere saydamlığı: %{0:0} opak", OpacitySlider.Value);

        WheelStepSlider.IsEnabled = !smart;
        SlowStepSlider.IsEnabled = smart;
        NormalStepSlider.IsEnabled = smart;
        FastStepSlider.IsEnabled = smart;
        TransitionSlider.IsEnabled = SmoothCheck.IsChecked == true;

        WheelStepLabel.Opacity = smart ? 0.5 : 1.0;
        SlowStepLabel.Opacity = NormalStepLabel.Opacity = FastStepLabel.Opacity = smart ? 1.0 : 0.5;
        TransitionLabel.Opacity = SmoothCheck.IsChecked == true ? 1.0 : 0.5;
    }

    private void DefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings
        {
            // Windows başlangıcı kullanıcı tercihi olarak korunur.
            StartWithWindows = _working.StartWithWindows,
        };

        _working.CopyPreferencesFrom(defaults);
        Load();
        SettingsChanged?.Invoke(this, _working.Clone());
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
