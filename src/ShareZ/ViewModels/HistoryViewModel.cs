using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareZ.Models;
using System.Collections.ObjectModel;

namespace ShareZ.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private HistoryItem? _selectedItem;

    public ObservableCollection<HistoryItem> Items { get; } = new();
    public ObservableCollection<HistoryItem> FilteredItems { get; } = new();

    public async Task LoadAsync()
    {
        await App.HistoryService.LoadAsync();
        Items.Clear();
        FilteredItems.Clear();

        foreach (var item in App.HistoryService.Items)
        {
            Items.Add(item);
            FilteredItems.Add(item);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilteredItems.Clear();
        var filtered = string.IsNullOrWhiteSpace(value)
            ? Items
            : new ObservableCollection<HistoryItem>(
                Items.Where(i => i.FileName.Contains(value, StringComparison.OrdinalIgnoreCase)));

        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }
    }

    [RelayCommand]
    private async Task DeleteItem()
    {
        if (SelectedItem != null)
        {
            await App.HistoryService.RemoveAsync(SelectedItem.Id);
            Items.Remove(SelectedItem);
            FilteredItems.Remove(SelectedItem);
            SelectedItem = null;
        }
    }

    [RelayCommand]
    private async Task ClearHistory()
    {
        await App.HistoryService.ClearAsync();
        Items.Clear();
        FilteredItems.Clear();
    }

    [RelayCommand]
    private void CopyUrl()
    {
        if (SelectedItem?.UploadUrl != null)
        {
            App.ClipboardService.CopyUrl(SelectedItem.UploadUrl);
        }
    }

    [RelayCommand]
    private void CopyFilePath()
    {
        if (SelectedItem != null)
        {
            App.ClipboardService.CopyFilePath(SelectedItem.FilePath);
        }
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (SelectedItem != null && File.Exists(SelectedItem.FilePath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SelectedItem.FilePath,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SelectedItem != null)
        {
            var folder = Path.GetDirectoryName(SelectedItem.FilePath);
            if (folder != null && Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select, \"{SelectedItem.FilePath}\"",
                    UseShellExecute = true
                });
            }
        }
    }
}
