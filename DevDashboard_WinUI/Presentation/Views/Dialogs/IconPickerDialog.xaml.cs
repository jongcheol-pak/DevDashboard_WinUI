using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class IconPickerDialog : ContentDialog
{
    private readonly IconPickerDialogViewModel _vm = new();
    private System.ComponentModel.PropertyChangedEventHandler? _vmPropertyChangedHandler;

    /// <summary>선택된 아이콘 글리프. null이면 선택 취소, 빈 문자열이면 기본값 복원.</summary>
    public string? SelectedGlyph { get; private set; }

    public IconPickerDialog()
    {
        InitializeComponent();

        Title = LocalizationService.Get("IconPickerDialogTitle");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");

        _vm.CloseRequested += OnVmCloseRequested;
        Closing += (_, _) =>
        {
            if (_vmPropertyChangedHandler is not null)
                _vm.PropertyChanged -= _vmPropertyChangedHandler;
            _vm.CloseRequested -= OnVmCloseRequested;
            _vm.Dispose();
        };
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        try
        {
            await _vm.LoadIconsAsync();

            LoadingRing.Visibility = Visibility.Collapsed;
            IconScrollViewer.Visibility = Visibility.Visible;

            IconRepeater.ItemsSource = _vm.FilteredIcons;
            UpdateCounts();

            _vmPropertyChangedHandler = (_, e) =>
            {
                if (e.PropertyName == nameof(IconPickerDialogViewModel.FilteredIcons))
                {
                    IconRepeater.ItemsSource = _vm.FilteredIcons;
                    UpdateCounts();
                }
            };
            _vm.PropertyChanged += _vmPropertyChangedHandler;
        }
        catch (Exception ex)
        {
            LoadingRing.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Format(LocalizationService.Get("UnexpectedError"), ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void UpdateCounts()
    {
        VisibleCountRun.Text = _vm.VisibleIconCount.ToString();
        TotalCountRun.Text = _vm.TotalIconCount.ToString();
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                await _vm.UpdateSearchAsync(sender.Text);
        }
        catch (Exception ex)
        {
            ErrorText.Text = string.Format(LocalizationService.Get("UnexpectedError"), ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
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
