using System.Text.Json;
using System.Text.Json.Serialization;

namespace CursorWorkflowHelper.Services;

public static class JsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static T LoadOrDefault<T>(string path, Func<T> factory) where T : class
    {
        try
        {
            if (!File.Exists(path))
                return factory();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options) ?? factory();
        }
        catch
        {
            return factory();
        }
    }

    public static void Save<T>(string path, T value)
    {
        AppPaths.EnsureRoot();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(path, json);
    }
}
