using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class ProjectHistoryDialog : WindowEx
{
    private const int MinW = 620;
    private const int InitW = 900;
    private const int InitH = 650;

    private ProjectHistoryDialogViewModel Vm { get; }
    private readonly TaskCompletionSource _closedTcs = new();

    public ProjectHistoryDialog(IReadOnlyList<ProjectItem> projects, IProjectRepository repository)
    {
        Vm = new ProjectHistoryDialogViewModel(projects.ToList(), repository);
        InitializeComponent();
        Title = LocalizationService.Get("ProjectHistoryDialogTitle");
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        Vm.PropertyChanged += (_, _) => RefreshList();
        RefreshList();

        // 창 닫힐 때 SaveAll 호출 후 Task 완료
        Closed += (_, _) =>
        {
            Vm.SaveAll();
            _closedTcs.TrySetResult();
        };
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private void RefreshList()
    {
        GroupList.ItemsSource = null;
        GroupList.ItemsSource = Vm.DateGroups;
        EmptyText.Visibility = !Vm.HasEntries ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            Vm.SearchText = sender.Text;
    }

    private void ToggleAddPanel_Click(object sender, RoutedEventArgs e)
    {
        AddPanel.Visibility = AddPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
        if (AddPanel.Visibility == Visibility.Visible)
        {
            AddTitleBox.Text = string.Empty;
            AddDescriptionBox.Text = string.Empty;
            AddErrorText.Visibility = Visibility.Collapsed;
            AddDatePicker.SelectedDate = DateTimeOffset.Now;
        }
    }

    private void SaveAdd_Click(object sender, RoutedEventArgs e)
    {
        var title = AddTitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            AddErrorText.Text = LocalizationService.Get("ProjectHistoryDialog_TitleRequired");
            AddErrorText.Visibility = Visibility.Visible;
            return;
        }

        AddErrorText.Visibility = Visibility.Collapsed;
        var date = AddDatePicker.SelectedDate?.Date ?? DateTime.Today;
        var desc = AddDescriptionBox.Text.Trim();
        var entry = new HistoryEntry { Title = title, Description = desc, CompletedAt = date };
        Vm.AddEntry(entry);
        AddPanel.Visibility = Visibility.Collapsed;
    }

    private void CancelAdd_Click(object sender, RoutedEventArgs e)
    {
        AddPanel.Visibility = Visibility.Collapsed;
    }

    private void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HistoryEntryViewModel entryVm })
            Vm.DeleteEntryCommand.Execute(entryVm);
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.SelectedProject is null) return;

        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add(LocalizationService.Get("HistoryDialog_MarkdownType"), [".md"]);
        picker.SuggestedFileName = $"{Vm.SelectedProject.Name}_history";

        // FileSavePicker hwnd는 이 다이얼로그 창 자체를 사용
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is not null)
            await FileIO.WriteTextAsync(file, Vm.ExportToMarkdown());
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
