using System.IO;
using System.Text.Json;

namespace Notari;

public sealed class AppSettings
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Notari", "settings.json");

    public string HighlightColor { get; set; } = "#66C084FC";
    public bool   DimBrackets    { get; set; } = true;
    public bool   DimSquare      { get; set; } = true;
    public bool   DimRound       { get; set; } = true;
    public bool   DimCurly       { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
