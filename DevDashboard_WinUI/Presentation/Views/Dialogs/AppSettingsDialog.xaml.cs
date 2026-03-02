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


        Vm.LoadFrom(settings);
        _initialLanguage = Vm.SelectedLanguageItem?.Value ?? LanguageSetting.SystemDefault;
        RefreshToolList();
        Vm.Tools.CollectionChanged += (_, _) => RefreshToolList();

        Closed += async (_, _) =>
        {
            await Vm.ApplyStartupTaskAsync(Vm.RunOnStartup);
            var settings = new AppSettings();
            Vm.ApplyTo(settings);
            // Close() 전에 확정한 언어 값으로 덮어쓰기 (XAML 소멸 시 바인딩 리셋 대응)
            if (_pendingLanguage is { } lang)
                settings.Language = lang;
            ResultSettings = settings;
            _closedTcs.TrySetResult();
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
        if (_initialized) return;
        _initialized = true;
        await Vm.LoadStartupStateAsync();
        await Vm.CheckLatestVersionAsync();
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
        if (Vm.SelectedLanguageItem is not { } langItem) return;

        // 초기 언어로 되돌린 경우 — 변경 플래그 초기화
        if (langItem.Value == _initialLanguage)
        {
            _pendingLanguage = null;
            LanguageChanged = false;
            App.ApplyLanguageSetting(_initialLanguage);
            LocalizationService.Reset();
            return;
        }

        // Close() 시 XAML 소멸로 TwoWay 바인딩이 SelectedLanguageItem을 null로 리셋하므로
        // 확정된 언어 값을 별도 필드에 보관
        _pendingLanguage = langItem.Value;
        App.ApplyLanguageSetting(langItem.Value);
        LocalizationService.Reset();
        LanguageChanged = true;
    }

    private async void ResetSettings_Click(object sender, RoutedEventArgs e)
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
    }

    private async void ResetProjects_Click(object sender, RoutedEventArgs e)
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
}
