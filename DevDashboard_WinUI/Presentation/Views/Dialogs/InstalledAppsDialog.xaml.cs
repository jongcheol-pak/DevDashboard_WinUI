using System.Diagnostics;
using DevDashboard.Infrastructure.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>
/// 현재 PC에 설치된 앱 목록을 표시하고 선택할 수 있는 다이얼로그.
/// </summary>
public sealed partial class InstalledAppsDialog : ContentDialog
{
    private List<InstalledAppInfo> _allApps = [];
    private CancellationTokenSource? _loadingCts;

    /// <summary>선택된 앱 정보. null이면 선택 취소.</summary>
    public InstalledAppInfo? ResultApp { get; private set; }

    public InstalledAppsDialog()
    {
        InitializeComponent();

        Title = LocalizationService.Get("InstalledAppsDialogTitle");
        PrimaryButtonText = LocalizationService.Get("Dialog_Select");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");
        IsPrimaryButtonEnabled = false;

        Closing += OnClosing;
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = new CancellationTokenSource();
        var ct = _loadingCts.Token;

        try
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            AppListView.Visibility = Visibility.Collapsed;
            SearchBox.IsEnabled = false;

            _allApps = await InstalledAppService.GetInstalledAppsAsync(ct);

            ct.ThrowIfCancellationRequested();

            AppListView.ItemsSource = _allApps;

            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            AppListView.Visibility = Visibility.Visible;
            SearchBox.IsEnabled = true;

            // 아이콘 비동기 로딩
            _ = LoadIconsAsync(_allApps, ct);
        }
        catch (OperationCanceledException)
        {
            // 취소됨
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"앱 목록 로드 오류: {ex.Message}");
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            AppListView.Visibility = Visibility.Visible;
            SearchBox.IsEnabled = true;
        }
    }

    private async Task LoadIconsAsync(List<InstalledAppInfo> apps, CancellationToken ct)
    {
        foreach (var app in apps)
        {
            if (ct.IsCancellationRequested) return;
            if (string.IsNullOrEmpty(app.IconPath)) continue;

            try
            {
                var bitmapImage = await IconCacheService.LoadImageAsync(app.IconPath, 32);
                if (bitmapImage is not null && !ct.IsCancellationRequested)
                    app.IconImage = bitmapImage;
            }
            catch { /* 개별 아이콘 로딩 실패 무시 */ }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            AppListView.ItemsSource = _allApps;
        }
        else
        {
            var filtered = _allApps
                .Where(a => a.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            AppListView.ItemsSource = filtered;
        }
    }

    private void AppListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = AppListView.SelectedItem is not null;
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (args.Result == ContentDialogResult.Primary)
        {
            ResultApp = AppListView.SelectedItem as InstalledAppInfo;
            if (ResultApp is null)
            {
                args.Cancel = true;
                return;
            }
        }

        // Primary/Close 모두 백그라운드 아이콘 로딩 중지
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = null;
    }
}
