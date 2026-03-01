using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class GroupDialog : WindowEx
{
    private const int MinW = 350;
    private const int InitW = 500;
    private const int InitH = 300;

    private readonly IReadOnlyList<ProjectGroup> _existingGroups;
    private GroupDialogViewModel Vm { get; } = new();
    private readonly TaskCompletionSource _closedTcs = new();

    public ProjectGroup? ResultGroup { get; private set; }

    public GroupDialog(IReadOnlyList<ProjectGroup> groups, ProjectGroup? existing)
    {
        _existingGroups = groups;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        if (existing is not null)
            Vm.LoadFrom(existing);

        // 추가/편집 모드에 따라 타이틀 설정
        Title = existing is null
            ? LocalizationService.Get("GroupDialog_TitleAdd")
            : LocalizationService.Get("GroupDialog_TitleEdit");

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

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var name = Vm.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError(LocalizationService.Get("GroupDialog_NameRequired"));
            return;
        }

        if (_existingGroups.Any(g =>
            g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            g.Id != Vm.EditingGroupId))
        {
            ShowError(string.Format(LocalizationService.Get("GroupDialog_DuplicateFormat"), name));
            return;
        }

        ResultGroup = new ProjectGroup
        {
            Id = Vm.EditingGroupId ?? Guid.NewGuid().ToString(),
            Name = name
        };
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
