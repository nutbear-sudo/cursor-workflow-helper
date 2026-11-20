namespace CursorWorkflowHelper.Models;

public sealed class AppSettings
{
    public bool DarkMode { get; set; }
    public bool AlwaysOnTop { get; set; }
    public Dictionary<string, bool> ChecklistState { get; set; } = new();
}
