using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using KahrimanVolumeMixer.Audio;
using KahrimanVolumeMixer.Core;
using KahrimanVolumeMixer.Themes;

namespace KahrimanVolumeMixer;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _store;
    private readonly MixerEngine _engine;
    private readonly TrayIcon _tray;
    private readonly DispatcherTimer _saveTimer;
    private SettingsWindow? _settingsWindow;
    private bool _exiting;
    private bool _windowReady;

    private int _shownPercent = -1;
    private bool? _shownMuted;
    private bool? _shownConnected;
    private string? _shownDevice;

    public MainWindow(AppSettings settings, SettingsStore store)
    {
        InitializeComponent();

        _settings = settings;
        _store = store;

        _saveTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1.5) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNow();
        };

        _engine = new MixerEngine(settings);
        _engine.StateChanged += (_, _) => RefreshUi();

        Fader.UserValueChanged += (_, level) =>
        {
            if (_engine.IsMuted && level > 0) _engine.SetMuted(false);
            _engine.SetLevel(level, smooth: false);
        };

        _tray = new TrayIcon();
        _tray.ToggleWindowRequested += (_, _) => ToggleVisibility();
        _tray.MuteRequested += (_, _) => _engine.ToggleMute();
        _tray.LowRequested += (_, _) => _engine.TogglePreset(_settings.LowLevelPercent);
        _tray.HighRequested += (_, _) => _engine.TogglePreset(_settings.HighLevelPercent);
        _tray.TopmostToggleRequested += (_, _) =>
        {
            var copy = _settings.Clone();
            copy.AlwaysOnTop = !_settings.AlwaysOnTop;
            ApplyPreferences(copy);
            _settingsWindow?.Reload(_settings);
        };
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.ExitRequested += (_, _) => ExitApplication();

        ThemeManager.ThemeChanged += OnThemeChanged;

        RestoreWindowPlacement();
        ApplyWindowPreferences();

        LocationChanged += (_, _) => OnPlacementChanged();
        SizeChanged += (_, _) => OnPlacementChanged();
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) => _windowReady = true;

        _engine.Start();
        RefreshUi(force: true);
    }

    // ------------------------------------------------------------------ Girdi

    private void RootBorder_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _engine.OnWheel(e.Delta, e.Timestamp);
        e.Handled = true;
    }

    private void RootBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            _engine.ToggleMute();
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                _engine.SetLevel(_engine.TargetLevel + (_settings.WheelStepPercent / 100.0), smooth: true);
                e.Handled = true;
                break;
            case Key.Down:
                _engine.SetLevel(_engine.TargetLevel - (_settings.WheelStepPercent / 100.0), smooth: true);
                e.Handled = true;
                break;
            case Key.M:
                _engine.ToggleMute();
                e.Handled = true;
                break;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // Fare tuşu bırakılmışsa DragMove hata verir; yok sayılır.
        }
    }

    private void LowButton_Click(object sender, RoutedEventArgs e) => _engine.TogglePreset(_settings.LowLevelPercent);

    private void HighButton_Click(object sender, RoutedEventArgs e) => _engine.TogglePreset(_settings.HighLevelPercent);

    private void MuteButton_Click(object sender, RoutedEventArgs e) => _engine.ToggleMute();

    /// <summary>Sağ tık: mevcut seviyeyi LOW olarak kaydet.</summary>
    private void LowButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var copy = _settings.Clone();
        copy.LowLevelPercent = VolumeMath.ToPercent(_engine.TargetLevel);
        ApplyPreferences(copy);
        _settingsWindow?.Reload(_settings);
        e.Handled = true;
    }

    /// <summary>Sağ tık: mevcut seviyeyi HIGH olarak kaydet.</summary>
    private void HighButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var copy = _settings.Clone();
        copy.HighLevelPercent = VolumeMath.ToPercent(_engine.TargetLevel);
        ApplyPreferences(copy);
        _settingsWindow?.Reload(_settings);
        e.Handled = true;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => ExitApplication();

    // ------------------------------------------------------------------ Arayüz

    private void RefreshUi(bool force = false)
    {
        double level = _engine.CurrentLevel;
        int percent = VolumeMath.ToPercent(level);
        bool muted = _engine.IsMuted;
        bool connected = _engine.IsConnected;
        string? device = _engine.DeviceName;

        Fader.Value = level;

        if (!force && percent == _shownPercent && muted == _shownMuted && connected == _shownConnected && device == _shownDevice)
        {
            return;
        }

        bool stateChanged = force || muted != _shownMuted || connected != _shownConnected || device != _shownDevice;
        _shownPercent = percent;
        _shownMuted = muted;
        _shownConnected = connected;
        _shownDevice = device;

        PercentText.Text = muted ? "MUTE" : percent + "%";

        if (stateChanged)
        {
            Fader.IsMuted = muted;
            Fader.IsConnected = connected;

            PercentText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, muted ? "AccentBrush" : "TextBrush");
            StatusDot.SetResourceReference(Shape.FillProperty, connected ? "OkBrush" : "OffBrush");
            string deviceLabel = string.IsNullOrWhiteSpace(device) ? "Varsayılan ses aygıtı" : device;
            StatusDot.ToolTip = connected ? $"Windows ana ses — {deviceLabel}" : "Ses çıkış aygıtı bulunamadı";
            StatusText.Text = connected ? "WINDOWS" : "AYGIT YOK";
            StatusText.ToolTip = connected ? deviceLabel : null;

            MuteGlyph.Text = muted ? "" : "";
            MuteButton.SetResourceReference(BackgroundProperty, muted ? "AccentDimBrush" : "ButtonBrush");
            UpdatePresetTooltips();
        }

        _tray.Update(percent, muted, connected, _settings.AlwaysOnTop);
        ScheduleSave();
    }

    private void UpdatePresetTooltips()
    {
        LowButton.ToolTip = $"LOW: %{_settings.LowLevelPercent:0} — tekrar tıkla: önceki seviye\nSağ tık: mevcut seviyeyi LOW yap";
        HighButton.ToolTip = $"HIGH: %{_settings.HighLevelPercent:0} — tekrar tıkla: önceki seviye\nSağ tık: mevcut seviyeyi HIGH yap";
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Fader.InvalidateVisual();
    }

    // ------------------------------------------------------------------ Pencere

    internal void ShowFromTray()
    {
        if (_exiting) return;
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        if (!_settings.AlwaysOnTop)
        {
            // Öne getirmek için kısa süreli üstte tut.
            Topmost = true;
            Topmost = false;
        }
    }

    private void ToggleVisibility()
    {
        if (IsVisible) Hide();
        else ShowFromTray();
    }

    private void RestoreWindowPlacement()
    {
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;

        if (_settings.WindowLeft is double left && _settings.WindowTop is double top && IsOnScreen(left, top, Width, Height))
        {
            Left = left;
            Top = top;
        }
        else
        {
            Rect work = SystemParameters.WorkArea;
            Left = work.Right - Width - 24;
            Top = work.Top + ((work.Height - Height) / 2.0);
        }
    }

    /// <summary>Kayıtlı konum bağlı monitörlerden birinde görünür mü (çoklu monitör / monitör çıkarılması)?</summary>
    private static bool IsOnScreen(double left, double top, double width, double height)
    {
        double vl = SystemParameters.VirtualScreenLeft;
        double vt = SystemParameters.VirtualScreenTop;
        double vr = vl + SystemParameters.VirtualScreenWidth;
        double vb = vt + SystemParameters.VirtualScreenHeight;

        const double margin = 30;
        return left + width - margin > vl
            && left + margin < vr
            && top >= vt - 10
            && top + margin < vb
            && height > 0;
    }

    private void ApplyWindowPreferences()
    {
        Topmost = _settings.AlwaysOnTop;
        Opacity = _settings.WindowOpacity;
    }

    private void OnPlacementChanged()
    {
        if (!_windowReady || WindowState != WindowState.Normal) return;
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.WindowWidth = ActualWidth > 0 ? ActualWidth : Width;
        _settings.WindowHeight = ActualHeight > 0 ? ActualHeight : Height;
        ScheduleSave();
    }

    // ------------------------------------------------------------------ Ayarlar

    private void OpenSettings()
    {
        ShowFromTray();

        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _store.FilePath);
        _settingsWindow.SettingsChanged += (_, updated) => ApplyPreferences(updated);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void ApplyPreferences(AppSettings updated)
    {
        bool startupChanged = updated.StartWithWindows != _settings.StartWithWindows;
        bool themeChanged = updated.Theme != _settings.Theme;

        _settings.CopyPreferencesFrom(updated);
        _engine.ApplySettings(_settings);
        ApplyWindowPreferences();

        if (themeChanged) ThemeManager.Apply(_settings.Theme);
        if (startupChanged) StartupManager.Apply(_settings.StartWithWindows);

        UpdatePresetTooltips();
        _tray.Update(VolumeMath.ToPercent(_engine.CurrentLevel), _engine.IsMuted, _engine.IsConnected, _settings.AlwaysOnTop);
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        _settings.LastLevel = _engine.TargetLevel;
        _settings.ReturnLevel = _engine.ReturnLevel;
        _store.Save(_settings);
    }

    // ------------------------------------------------------------------ Kapatma

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 de güvenli çıkış yapar.
        _exiting = true;
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _saveTimer.Stop();
        ThemeManager.ThemeChanged -= OnThemeChanged;
        _settingsWindow?.Close();

        // Güvenli kapatma: Windows ses seviyesine dokunulmaz, yalnızca kaynaklar bırakılır.
        _engine.Dispose();
        SaveNow();
        _tray.Dispose();

        Application.Current.Shutdown();
    }
}
