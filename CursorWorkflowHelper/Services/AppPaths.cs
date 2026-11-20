namespace CursorWorkflowHelper.Services;

/// <summary>本地数据目录：%LocalAppData%\CursorWorkflowHelper\</summary>
public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorWorkflowHelper");

    public static string HistoryFile => Path.Combine(Root, "history.json");
    public static string TemplatesFile => Path.Combine(Root, "templates.json");
    public static string SettingsFile => Path.Combine(Root, "settings.json");

    public static void EnsureRoot()
    {
        Directory.CreateDirectory(Root);
    }
}
