using System.IO;
using System.Text.Json;

namespace Notari;

public sealed class AppSettings
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Notari", "settings.json");

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string AccentColor          { get; set; } = "#FF4FC3F7";
    public bool   HasShownStartMessage { get; set; } = false;
    public bool   DimBrackets    { get; set; } = true;
    public bool   DimSquare      { get; set; } = true;
    public bool   DimRound       { get; set; } = true;
    public bool   DimCurly       { get; set; } = false;

    public bool SortByZipf             { get; set; } = false;
    public int  ResultLimit             { get; set; } = 0;
    public bool AutoSave                { get; set; } = false;
    public int  AutoSaveIntervalSeconds { get; set; } = 300;
    public bool ShowNotes               { get; set; } = true;
    public bool ShowPhoneticSection     { get; set; } = true;
    public bool ShowSemanticSection     { get; set; } = true;
    public int  LookupDebounceMs        { get; set; } = 250;
    public bool TypewriterMode          { get; set; } = false;
    public bool ShowDebugLabels         { get; set; } = false;

    /// <summary>
    /// Loads settings from disk. If the file is missing, returns defaults.
    /// If the file is present but unreadable or corrupt, renames it to .bak
    /// (preserving it for diagnostics) and returns defaults.
    /// </summary>
    public static AppSettings Load()
    {
        if (!File.Exists(_path)) return new();

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new();
        }
        catch
        {
            // Back up the corrupted file so it doesn't keep blocking load on every launch.
            try { File.Move(_path, _path + ".bak", overwrite: true); } catch { }
            return new();
        }
    }

    /// <summary>
    /// Persists settings to disk. Returns <see langword="false"/> if the write fails.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(this, _jsonOptions));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
