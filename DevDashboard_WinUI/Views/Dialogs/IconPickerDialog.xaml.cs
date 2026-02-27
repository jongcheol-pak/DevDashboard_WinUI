using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Views.Dialogs;

public sealed partial class IconPickerDialog : ContentDialog
{
    private readonly IconPickerDialogViewModel _vm = new();

    /// <summary>선택된 아이콘 글리프. null이면 선택 취소, 빈 문자열이면 기본값 복원.</summary>
    public string? SelectedGlyph { get; private set; }

    public IconPickerDialog()
    {
        InitializeComponent();
        Opened += OnOpened;
        _vm.CloseRequested += OnVmCloseRequested;
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await _vm.LoadIconsAsync();

        LoadingRing.Visibility = Visibility.Collapsed;
        IconScrollViewer.Visibility = Visibility.Visible;

        IconRepeater.ItemsSource = _vm.FilteredIcons;
        UpdateCounts();

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IconPickerDialogViewModel.FilteredIcons))
            {
                IconRepeater.ItemsSource = _vm.FilteredIcons;
                UpdateCounts();
            }
        };
    }

    private void UpdateCounts()
    {
        VisibleCountRun.Text = _vm.VisibleIconCount.ToString();
        TotalCountRun.Text = _vm.TotalIconCount.ToString();
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            await _vm.UpdateSearchAsync(sender.Text);
    }

    private void IconButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: IconItem icon })
            _vm.SelectIconCommand.Execute(icon);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.ResetToDefaultCommand.Execute(null);
    }

    private void OnVmCloseRequested(object? sender, EventArgs e)
    {
        if (_vm.IsConfirmed)
            SelectedGlyph = _vm.SelectedGlyph;
        Hide();
    }
}
