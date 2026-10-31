using System.Text.Json;
using System.Text.Json.Serialization;

namespace KahrimanVolumeMixer.Core;

/// <summary>Ayarları JSON dosyasına güvenli (geçici dosya + atomik taşıma) şekilde kaydeder.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public SettingsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Dosya yolu boş olamaz.", nameof(filePath));
        FilePath = filePath;
    }

    /// <summary>%APPDATA%\KahrimanVolumeMixer\settings.json</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KahrimanVolumeMixer",
        "settings.json");

    public string FilePath { get; }

    /// <summary>Ayarları yükler. Dosya yoksa veya bozuksa varsayılanları döndürür; asla istisna fırlatmaz.</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            string json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>Ayarları kaydeder. Başarısızsa false döner; asla istisna fırlatmaz.</summary>
    public bool Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
            File.Move(temp, FilePath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
