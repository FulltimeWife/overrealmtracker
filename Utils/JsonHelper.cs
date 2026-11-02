using System.Text.Json;

namespace OverrealmTracker.Utils;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static T? Load<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(text, Options);
        }
        catch
        {
            return null;
        }
    }

    public static void Save<T>(string path, T data)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(data, Options));
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }
}
