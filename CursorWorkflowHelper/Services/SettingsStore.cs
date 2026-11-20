using CursorWorkflowHelper.Models;

namespace CursorWorkflowHelper.Services;

public sealed class SettingsStore
{
    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        Current = JsonStore.LoadOrDefault(AppPaths.SettingsFile, () => new AppSettings());
    }

    public void Save()
    {
        JsonStore.Save(AppPaths.SettingsFile, Current);
    }
}
