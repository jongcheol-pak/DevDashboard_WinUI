using DevDashboard.Models;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Views.Dialogs;

public sealed partial class AppSettingsDialog : ContentDialog
{
    private AppSettingsDialogViewModel Vm { get; } = new();

    public AppSettings? ResultSettings { get; private set; }

    public AppSettingsDialog(AppSettings settings)
    {
        InitializeComponent();
        Vm.LoadFrom(settings);
        RefreshToolList();
        Vm.Tools.CollectionChanged += (_, _) => RefreshToolList();
        PrimaryButtonClick += OnPrimaryButtonClick;
        Opened += async (_, _) => await Vm.CheckLatestVersionAsync();
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

    // ─── 저장 ────────────────────────────────────────────

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var settings = new AppSettings();
        Vm.ApplyTo(settings);
        ResultSettings = settings;
    }
}
