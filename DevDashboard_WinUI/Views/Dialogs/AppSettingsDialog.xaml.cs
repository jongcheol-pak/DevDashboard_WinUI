using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Views.Dialogs;

public sealed partial class AppSettingsDialog : WindowEx
{
  
    private const int InitW = 800;
    private const int InitH = 640;

    private AppSettingsDialogViewModel Vm { get; } = new();
    private readonly TaskCompletionSource _closedTcs = new();

    public AppSettings? ResultSettings { get; private set; }

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
        RefreshToolList();
        Vm.Tools.CollectionChanged += (_, _) => RefreshToolList();

        Closed += (_, _) => _closedTcs.TrySetResult();
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

    // ─── 저장/취소 ───────────────────────────────────────

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var settings = new AppSettings();
        Vm.ApplyTo(settings);
        ResultSettings = settings;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
