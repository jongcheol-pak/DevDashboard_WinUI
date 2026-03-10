using DevDashboard.Infrastructure.Persistence;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class AppSettingsDialog : ContentDialog
{
    private AppSettingsDialogViewModel Vm { get; } = new();
    private readonly System.Collections.Specialized.NotifyCollectionChangedEventHandler _toolsCollectionChangedHandler;
    private readonly System.ComponentModel.PropertyChangedEventHandler _vmPropertyChangedHandler;

    private TaskCompletionSource<bool>? _nestedTcs;
    public AppSettings? ResultSettings { get; private set; }

    /// <summary>프로젝트 초기화가 수행되었는지 여부 (호출자가 목록 새로고침에 사용)</summary>
    public bool ProjectsReset { get; private set; }

    /// <summary>설정 초기화가 수행되었는지 여부 (호출자가 그룹 초기화에 사용)</summary>
    public bool SettingsReset { get; private set; }

    /// <summary>런처 초기화가 수행되었는지 여부 (호출자가 사이드바 새로고침에 사용)</summary>
    public bool LauncherReset { get; private set; }

    /// <summary>언어가 변경되었는지 여부 (호출자가 UI 새로고침에 사용)</summary>
    public bool LanguageChanged { get; private set; }

    private LanguageSetting _initialLanguage;

    /// <summary>Close() 전에 확정된 언어 값 — XAML 소멸 시 바인딩 리셋으로 유실 방지</summary>
    private LanguageSetting? _pendingLanguage;

    public AppSettingsDialog(AppSettings settings)
    {
        InitializeComponent();

        Title = LocalizationService.Get("AppSettingsDialogTitle");
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        _initialLanguage = settings.Language;
        Vm.LoadFrom(settings);
        RefreshToolList();
        _toolsCollectionChangedHandler = (_, _) => RefreshToolList();
        Vm.Tools.CollectionChanged += _toolsCollectionChangedHandler;

        // 테마 변경을 즉시 반영
        _vmPropertyChangedHandler = (_, e) =>
        {
            if (e.PropertyName == nameof(AppSettingsDialogViewModel.SelectedThemeModeItem)
                && Vm.SelectedThemeModeItem is { } item)
            {
                this.RequestedTheme = AppSettingsDialogViewModel.ToElementTheme(item.Value);
            }
        };
        Vm.PropertyChanged += _vmPropertyChangedHandler;

        Closing += (_, _) =>
        {
            // 이벤트 핸들러 해제
            Vm.Tools.CollectionChanged -= _toolsCollectionChangedHandler;
            Vm.PropertyChanged -= _vmPropertyChangedHandler;

            // 결과 확정 (동기 전용 — Closing은 deferral 미지원)
            var resultSettings = new AppSettings();
            Vm.ApplyTo(resultSettings);
            // Close() 전에 확정한 언어 값으로 덮어쓰기 (XAML 소멸 시 바인딩 리셋 대응)
            if (_pendingLanguage is { } lang)
                resultSettings.Language = lang;
            ResultSettings = resultSettings;
        };
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        ContentDialogResult result;
        do
        {
            result = await base.ShowAsync();
            if (_nestedTcs is not null)
            {
                await _nestedTcs.Task;
                _nestedTcs = null;
                continue;
            }
            break;
        } while (true);

        // StartupTask API는 OS 호출이므로 Dialog 닫힌 후 처리 (실패해도 결과 설정은 보존)
        try { await Vm.ApplyStartupTaskAsync(Vm.RunOnStartup); }
        catch { /* StartupTask 실패 무시 */ }

        return result;
    }

    private async Task<ContentDialogResult> ShowNestedDialogAsync(ContentDialog dialog)
    {
        _nestedTcs = new TaskCompletionSource<bool>();
        Hide();
        dialog.XamlRoot = App.MainWindow?.Content?.XamlRoot;
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _nestedTcs.TrySetResult(true);
        }
    }

    private Task ShowNestedErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("Dialog_DefaultErrorTitle"),
            Content = string.Format(LocalizationService.Get("UnexpectedError"), message),
            CloseButtonText = LocalizationService.Get("Dialog_OK"),
        };
        return ShowNestedDialogAsync(dialog);
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        try
        {
            await Vm.LoadStartupStateAsync();
            _ = Vm.CheckLatestVersionAsync();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
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

    // --- 도구 패널 ---

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

    // --- 코드 패널 ---

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

    // --- 정보 패널 ---

    private void OpenLicenseUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: string url })
            Vm.OpenLicenseUrlCommand.Execute(url);
    }

    // --- 설정 패널 ---

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not LanguageItem langItem) return;

        // 초기 언어로 되돌린 경우 — 변경 플래그 초기화
        if (langItem.Value == _initialLanguage)
        {
            _pendingLanguage = null;
            LanguageChanged = false;
            return;
        }

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
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            var defaultSettings = new AppSettings();
            Vm.LoadFrom(defaultSettings);
            new JsonStorageService().Save(defaultSettings);
            RefreshToolList();
            SettingsReset = true;
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
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
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            DatabaseContext.ClearProjectData();

            ProjectsReset = true;
            Hide();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void ResetLauncher_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("ResetLauncherConfirmTitle"),
                Content = LocalizationService.Get("ResetLauncherConfirmMessage"),
                PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
                CloseButtonText = LocalizationService.Get("Dialog_No"),
                DefaultButton = ContentDialogButton.Close,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            DatabaseContext.ClearLauncherData();

            LauncherReset = true;
            Hide();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }
}
