using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

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

        if (existing is not null)
        {
            Vm.LoadFrom(existing, groups, tools);
            Vm.SetExistingNames(existingNames);
            Title = LocalizationService.Get("ProjectSettingsDialog_TitleEdit");
        }
        else
        {
            Vm.SetExistingNames(existingNames);
            Title = LocalizationService.Get("ProjectSettingsDialog_TitleAdd");
            if (!string.IsNullOrEmpty(defaultGroupId))
                Vm.SelectedGroupId = defaultGroupId;
        }

        PrimaryButtonText = LocalizationService.Get("Dialog_Save");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private void OnSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        ResultItem = Vm.ToProjectItem();
    }
}
