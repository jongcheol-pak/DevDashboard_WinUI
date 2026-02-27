using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Views.Dialogs;

public sealed partial class ProjectSettingsDialog : ContentDialog
{
    private ProjectSettingsDialogViewModel Vm { get; } = new();

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

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        ResultItem = Vm.ToProjectItem();
    }
}
