using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using KahrimanVolumeMixer.Core;

namespace KahrimanVolumeMixer.Themes;

/// <summary>Koyu/açık temayı uygulama kaynaklarındaki renkleri değiştirerek uygular (DynamicResource ile anında güncellenir).</summary>
internal static class ThemeManager
{
    private static readonly IReadOnlyDictionary<string, string> Dark = new Dictionary<string, string>
    {
        ["BgBrush"] = "#161618",
        ["PanelBrush"] = "#202024",
        ["TrackBrush"] = "#2A2A30",
        ["LineBrush"] = "#3A3A42",
        ["TextBrush"] = "#F2F2F2",
        ["SubTextBrush"] = "#8C8C96",
        ["AccentBrush"] = "#FF8A1F",
        ["AccentDimBrush"] = "#7A4A1C",
        ["ButtonBrush"] = "#26262B",
        ["ButtonHoverBrush"] = "#33333A",
        ["CapBrush"] = "#E8E8EC",
        ["OkBrush"] = "#3DDC84",
        ["OffBrush"] = "#6B6B73",
    };

    private static readonly IReadOnlyDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["BgBrush"] = "#F4F4F6",
        ["PanelBrush"] = "#FFFFFF",
        ["TrackBrush"] = "#DCDCE2",
        ["LineBrush"] = "#C4C4CC",
        ["TextBrush"] = "#1A1A1E",
        ["SubTextBrush"] = "#6A6A74",
        ["AccentBrush"] = "#F07800",
        ["AccentDimBrush"] = "#F5C08A",
        ["ButtonBrush"] = "#E8E8ED",
        ["ButtonHoverBrush"] = "#DADAE1",
        ["CapBrush"] = "#FFFFFF",
        ["OkBrush"] = "#16A34A",
        ["OffBrush"] = "#A0A0A8",
    };

    public static event EventHandler? ThemeChanged;

    public static AppTheme Current { get; private set; } = AppTheme.Dark;

    public static void Apply(AppTheme theme)
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        var palette = theme == AppTheme.Light ? Light : Dark;
        foreach (var pair in palette)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pair.Value));
            brush.Freeze();
            resources[pair.Key] = brush;
        }

        Current = theme;
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}
