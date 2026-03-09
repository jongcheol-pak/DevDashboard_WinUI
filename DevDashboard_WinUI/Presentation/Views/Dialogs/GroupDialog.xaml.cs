using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class GroupDialog : ContentDialog
{
    private readonly IReadOnlyList<ProjectGroup> _existingGroups;
    private GroupDialogViewModel Vm { get; } = new();

    public ProjectGroup? ResultGroup { get; private set; }

    public GroupDialog(IReadOnlyList<ProjectGroup> groups, ProjectGroup? existing)
    {
        _existingGroups = groups;
        InitializeComponent();

        if (existing is not null)
            Vm.LoadFrom(existing);

        Title = existing is null
            ? LocalizationService.Get("GroupDialog_TitleAdd")
            : LocalizationService.Get("GroupDialog_TitleEdit");

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
        var name = Vm.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError(LocalizationService.Get("GroupDialog_NameRequired"));
            args.Cancel = true;
            return;
        }

        if (_existingGroups.Any(g =>
            g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            g.Id != Vm.EditingGroupId))
        {
            ShowError(string.Format(LocalizationService.Get("GroupDialog_DuplicateFormat"), name));
            args.Cancel = true;
            return;
        }

        ResultGroup = new ProjectGroup
        {
            Id = Vm.EditingGroupId ?? Guid.NewGuid().ToString(),
            Name = name
        };
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
