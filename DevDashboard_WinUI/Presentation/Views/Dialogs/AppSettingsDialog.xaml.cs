using DevDashboard.Infrastructure.Persistence;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class AppSettingsDialog : WindowEx
{
  
    private const int InitW = 800;
    private const int InitH = 640;

    private AppSettingsDialogViewModel Vm { get; } = new();
    private readonly TaskCompletionSource _closedTcs = new();

    public AppSettings? ResultSettings { get; private set; }

    /// <summary>프로젝트 초기화가 수행되었는지 여부 (호출자가 목록 새로고침에 사용)</summary>
    public bool ProjectsReset { get; private set; }

    /// <summary>설정 초기화가 수행되었는지 여부 (호출자가 그룹 초기화에 사용)</summary>
    public bool SettingsReset { get; private set; }

    /// <summary>언어가 변경되었는지 여부 (호출자가 UI 새로고침에 사용)</summary>
    public bool LanguageChanged { get; private set; }

    private LanguageSetting _initialLanguage;

    /// <summary>Close() 전에 확정된 언어 값 — XAML 소멸 시 바인딩 리셋으로 유실 방지</summary>
    private LanguageSetting? _pendingLanguage;

    public AppSettingsDialog(AppSettings settings)
    {
        InitializeComponent();
        Title = LocalizationService.Get("AppSettingsDialogTitle");
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        var manager = WindowManager.Get(this);


        _initialLanguage = settings.Language;
        Vm.LoadFrom(settings);
        RefreshToolList();
        Vm.Tools.CollectionChanged += (_, _) => RefreshToolList();

        // 팝업 창 자체에도 테마 변경을 즉시 반영
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppSettingsDialogViewModel.SelectedThemeModeItem)
                && Vm.SelectedThemeModeItem is { } item
                && Content is FrameworkElement root)
            {
                root.RequestedTheme = AppSettingsDialogViewModel.ToElementTheme(item.Value);
            }
        };

        Closed += async (_, _) =>
        {
            // 결과를 먼저 확정하고 TCS 즉시 해제 → 호출자(MainWindow)가 지연 없이 실행됨
            var settings = new AppSettings();
            Vm.ApplyTo(settings);
            // Close() 전에 확정한 언어 값으로 덮어쓰기 (XAML 소멸 시 바인딩 리셋 대응)
            if (_pendingLanguage is { } lang)
                settings.Language = lang;
            ResultSettings = settings;
            _closedTcs.TrySetResult();

            // StartupTask API는 OS 호출이므로 TCS 해제 후 별도로 처리
            await Vm.ApplyStartupTaskAsync(Vm.RunOnStartup);
        };
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private bool _initialized;
    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_initialized) return;
            _initialized = true;
            await Vm.LoadStartupStateAsync();
            _ = Vm.CheckLatestVersionAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private void RefreshToolList()
    {
        ToolList.ItemsSource = null;
        ToolList.ItemsSource = Vm.Tools;
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string indexStr } || !int.TryParse(indexStr, out var index))
            return;

        PanelSettings.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelTools.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelCode.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        PanelInfo.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ─── 도구 패널 ───────────────────────────────────────

    private void AddTool_Click(object sender, RoutedEventArgs e)
    {
        Vm.AddToolCommand.Execute(null);
        RefreshToolList();
    }

    private void ToolEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ToolItemViewModel item })
        {
            item.StartEditCommand.Execute(null);
            RefreshToolList();
        }
    }

    private void ToolDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ToolItemViewModel item })
        {
            item.DeleteCommand.Execute(null);
            RefreshToolList();
        }
    }

    private void ToolSaveEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ToolItemViewModel item })
        {
            item.SaveEditCommand.Execute(null);
            RefreshToolList();
        }
    }

    private void ToolCancelEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ToolItemViewModel item })
        {
            item.CancelEditCommand.Execute(null);
            RefreshToolList();
        }
    }

    // ─── 코드 패널 ───────────────────────────────────────

    private void RemoveTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
            Vm.RemoveTechTagCommand.Execute(tag);
    }

    private void RemoveCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string category })
            Vm.RemoveCategoryCommand.Execute(category);
    }

    // ─── 정보 패널 ───────────────────────────────────────

    private void OpenLicenseUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: string url })
            Vm.OpenLicenseUrlCommand.Execute(url);
    }

    // ─── 설정 패널 ───────────────────────────────────────

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Vm.SelectedLanguageItem 대신 e.AddedItems 사용:
        // x:Bind TwoWay 내부 핸들러와 실행 순서가 보장되지 않아 ViewModel이 아직 이전 값일 수 있음
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not LanguageItem langItem) return;

        // 초기 언어로 되돌린 경우 — 변경 플래그 초기화
        if (langItem.Value == _initialLanguage)
        {
            _pendingLanguage = null;
            LanguageChanged = false;
            return;
        }

        // 재시작 방식으로 적용하므로 선택 시점에는 플래그만 기록.
        // (즉시 ApplyLanguageSetting/Reset 호출 시 ResourceContext 재평가로 검은 화면 + 느림 유발)
        // Close() 시 XAML 소멸로 TwoWay 바인딩이 SelectedLanguageItem을 null로 리셋하므로
        // 확정된 언어 값을 별도 필드에 보관
        _pendingLanguage = langItem.Value;
        LanguageChanged = true;
    }

    private async void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("ResetSettingsConfirmTitle"),
                Content = LocalizationService.Get("ResetSettingsConfirmMessage"),
                PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
                CloseButtonText = LocalizationService.Get("Dialog_No"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var defaultSettings = new AppSettings();
            Vm.LoadFrom(defaultSettings);
            new JsonStorageService().Save(defaultSettings);
            RefreshToolList();
            SettingsReset = true;
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private async void ResetProjects_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("ResetProjectsConfirmTitle"),
                Content = LocalizationService.Get("ResetProjectsConfirmMessage"),
                PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
                CloseButtonText = LocalizationService.Get("Dialog_No"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var dbPath = DatabaseContext.GetDbPath();
            SqliteConnection.ClearAllPools();

            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(dbPath)!, Path.GetFileName(dbPath) + "*"))
            {
                try { File.Delete(file); }
                catch { /* WAL/SHM 파일 삭제 실패 시 무시 */ }
            }

            ProjectsReset = true;
            Close();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }
}
