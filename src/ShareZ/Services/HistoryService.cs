using System.Text.Json;
using ShareZ.Models;

namespace ShareZ.Services;

public class HistoryService
{
    private readonly string _historyFolder;
    private readonly string _historyFile;
    private List<HistoryItem> _items = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IReadOnlyList<HistoryItem> Items => _items.AsReadOnly();

    public event EventHandler? HistoryChanged;

    public HistoryService()
    {
        _historyFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShareZ");
        _historyFile = Path.Combine(_historyFolder, "history.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_historyFile))
            {
                var json = await File.ReadAllTextAsync(_historyFile);
                _items = JsonSerializer.Deserialize<List<HistoryItem>>(json, JsonOptions) ?? new();
            }
        }
        catch
        {
            _items = new();
        }
    }

    public async Task AddAsync(HistoryItem item)
    {
        _items.Insert(0, item);
        await SaveAsync();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveAsync(string id)
    {
        _items.RemoveAll(x => x.Id == id);
        await SaveAsync();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ClearAsync()
    {
        _items.Clear();
        await SaveAsync();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveAsync()
    {
        Directory.CreateDirectory(_historyFolder);
        var json = JsonSerializer.Serialize(_items, JsonOptions);
        await File.WriteAllTextAsync(_historyFile, json);
    }
}
