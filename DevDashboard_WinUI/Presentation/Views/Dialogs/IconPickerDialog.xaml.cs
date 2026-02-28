using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class IconPickerDialog : WindowEx
{
    private const int MinW = 580;
    private const int InitW = 700;
    private const int InitH = 500;

    private readonly IconPickerDialogViewModel _vm = new();
    private readonly TaskCompletionSource _closedTcs = new();
    private bool _initialized;

    /// <summary>선택된 아이콘 글리프. null이면 선택 취소, 빈 문자열이면 기본값 복원.</summary>
    public string? SelectedGlyph { get; private set; }

    public IconPickerDialog()
    {
        InitializeComponent();
        Title = LocalizationService.Get("IconPickerDialogTitle");
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        _vm.CloseRequested += OnVmCloseRequested;
        Closed += (_, _) => _closedTcs.TrySetResult();
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

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
        Close();
    }
}
