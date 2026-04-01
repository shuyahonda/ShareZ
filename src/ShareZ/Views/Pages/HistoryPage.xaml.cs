using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.Models;
using ShareZ.ViewModels;

namespace ShareZ.Views.Pages;

public sealed partial class HistoryPage : Page
{
    private readonly HistoryViewModel _viewModel = new();

    public HistoryPage()
    {
        this.InitializeComponent();
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        await _viewModel.LoadAsync();
        HistoryList.ItemsSource = _viewModel.FilteredItems;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchQuery = SearchBox.Text;
        HistoryList.ItemsSource = _viewModel.FilteredItems;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
    }

    private async void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Clear History",
            Content = "Are you sure you want to clear all history? This cannot be undone.",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _viewModel.ClearHistoryCommand.ExecuteAsync(null);
            HistoryList.ItemsSource = _viewModel.FilteredItems;
        }
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedItem = HistoryList.SelectedItem as HistoryItem;
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is HistoryItem item)
        {
            _viewModel.SelectedItem = item;
            _viewModel.OpenFileCommand.Execute(null);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is HistoryItem item)
        {
            _viewModel.SelectedItem = item;
            _viewModel.OpenFolderCommand.Execute(null);
        }
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is HistoryItem item)
        {
            _viewModel.SelectedItem = item;
            _viewModel.CopyFilePathCommand.Execute(null);
        }
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is HistoryItem item)
        {
            _viewModel.SelectedItem = item;
            await _viewModel.DeleteItemCommand.ExecuteAsync(null);
            HistoryList.ItemsSource = _viewModel.FilteredItems;
        }
    }
}
