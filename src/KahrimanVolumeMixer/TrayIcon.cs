using System;
using System.Reflection;

namespace KahrimanVolumeMixer;

/// <summary>Sistem tepsisi simgesi ve menüsü (WinForms NotifyIcon).</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.ContextMenuStrip _menu;
    private readonly System.Windows.Forms.ToolStripMenuItem _muteItem;
    private readonly System.Windows.Forms.ToolStripMenuItem _topmostItem;
    private readonly System.Drawing.Icon _icon;
    private string _lastText = string.Empty;

    public TrayIcon()
    {
        _icon = LoadIcon();

        _menu = new System.Windows.Forms.ContextMenuStrip();
        _menu.Items.Add("Göster / Gizle", null, (_, _) => ToggleWindowRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _muteItem = new System.Windows.Forms.ToolStripMenuItem("Sessize al", null, (_, _) => MuteRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(_muteItem);
        _menu.Items.Add("LOW", null, (_, _) => LowRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add("HIGH", null, (_, _) => HighRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _topmostItem = new System.Windows.Forms.ToolStripMenuItem("Her zaman üstte", null, (_, _) => TopmostToggleRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(_topmostItem);
        _menu.Items.Add("Ayarlar…", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _menu.Items.Add("Çıkış", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = _icon,
            Text = "KAHRIMAN VOLUME MIXER",
            ContextMenuStrip = _menu,
            Visible = true,
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) ToggleWindowRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? ToggleWindowRequested;
    public event EventHandler? MuteRequested;
    public event EventHandler? LowRequested;
    public event EventHandler? HighRequested;
    public event EventHandler? TopmostToggleRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public void Update(int percent, bool muted, bool connected, bool topmost)
    {
        string state = !connected ? "Chrome bulunamadı" : muted ? "Sessiz" : $"%{percent}";
        string text = $"KAHRIMAN VOLUME MIXER — {state}";
        if (text != _lastText)
        {
            _lastText = text;
            _notifyIcon.Text = text.Length > 120 ? text.Substring(0, 120) : text;
        }

        _muteItem.Text = muted ? "Sesi aç" : "Sessize al";
        _topmostItem.Checked = topmost;
    }

    private static System.Drawing.Icon LoadIcon()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
            if (stream is not null)
            {
                return new System.Drawing.Icon(stream, System.Windows.Forms.SystemInformation.SmallIconSize);
            }
        }
        catch
        {
            // Varsayılan simgeye düşülür.
        }

        return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon.Dispose();
    }
}
