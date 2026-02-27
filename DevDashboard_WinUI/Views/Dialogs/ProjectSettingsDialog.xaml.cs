using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Views.Dialogs;

public sealed partial class ProjectSettingsDialog : Window
{
    private const int MinW = 600;
    private const int InitW = 800;
    private const int InitH = 650;

    private ProjectSettingsDialogViewModel Vm { get; } = new();
    private readonly TaskCompletionSource _closedTcs = new();

    public ProjectItem? ResultItem { get; private set; }

    public ProjectSettingsDialog(
        IReadOnlyList<ProjectGroup> groups,
        IReadOnlyList<ExternalTool> tools,
        IReadOnlyList<string> existingNames,
        AppSettings settings,
        ProjectItem? existing,
        string? defaultGroupId)
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        Vm.LoadGroups(groups);
        Vm.LoadTools(tools);
        Vm.LoadCategories(settings.Categories);
        Vm.LoadCustomTags(settings.TechStackTags);
        Vm.SetExistingNames(existingNames);

        if (existing is not null)
        {
            Vm.LoadFrom(existing, groups, tools);
            Title = LocalizationService.Get("ProjectSettingsDialog_TitleEdit");
        }
        else
        {
            Title = LocalizationService.Get("ProjectSettingsDialog_TitleAdd");
            if (!string.IsNullOrEmpty(defaultGroupId))
                Vm.SelectedGroupId = defaultGroupId;
        }

        Closed += (_, _) => _closedTcs.TrySetResult();
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        ResultItem = Vm.ToProjectItem();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
