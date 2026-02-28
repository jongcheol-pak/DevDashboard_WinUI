using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class ProjectSettingsDialog : WindowEx
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
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        Vm.LoadGroups(groups);
        Vm.LoadTools(tools);
        Vm.LoadCategories(settings.Categories);
        Vm.LoadCustomTags(settings.TechStackTags);

        if (existing is not null)
        {
            Vm.LoadFrom(existing, groups, tools);
            // LoadFrom 이후 호출해야 EditingProjectId가 설정되어 자기 자신 이름이 중복 목록에서 제외됨
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

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        Closed += (_, _) => _closedTcs.TrySetResult();
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("InputRequired"),
                Content = error,
                CloseButtonText = LocalizationService.Get("Btn_Close.Content"),
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        ResultItem = Vm.ToProjectItem();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
