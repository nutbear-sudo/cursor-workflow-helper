using CursorWorkflowHelper.Models;

namespace CursorWorkflowHelper.Services;

public sealed class HistoryStore
{
    private const int MaxItems = 50;
    private List<TaskBrief> _items = [];

    public IReadOnlyList<TaskBrief> Items => _items;

    public void Load()
    {
        _items = JsonStore.LoadOrDefault(AppPaths.HistoryFile, () => new List<TaskBrief>());
        _items = _items
            .OrderByDescending(x => x.UpdatedAt)
            .Take(MaxItems)
            .ToList();
    }

    public void Save()
    {
        JsonStore.Save(AppPaths.HistoryFile, _items);
    }

    public void Upsert(TaskBrief brief)
    {
        brief.UpdatedAt = DateTime.Now;
        var idx = _items.FindIndex(x => x.Id == brief.Id);
        if (idx >= 0)
            _items[idx] = brief.Clone();
        else
            _items.Insert(0, brief.Clone());

        _items = _items
            .OrderByDescending(x => x.UpdatedAt)
            .Take(MaxItems)
            .ToList();
        Save();
    }

    public void Delete(string id)
    {
        _items.RemoveAll(x => x.Id == id);
        Save();
    }

    public void Clear()
    {
        _items.Clear();
        Save();
    }
}
