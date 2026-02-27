using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUIEx;

namespace DevDashboard.Views.Dialogs;

public sealed partial class HistoryDialog : WindowEx
{
    private const int MinW = 580;
    private const int InitW = 800;
    private const int InitH = 600;

    private HistoryDialogViewModel Vm { get; }
    private readonly TaskCompletionSource _closedTcs = new();

    public HistoryDialog(HistoryDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        // 프로젝트명을 포함한 동적 타이틀 설정
        Title = string.Format(LocalizationService.Get("HistoryDialog_TitleFormat"), vm.ProjectName);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        Vm.PropertyChanged += (_, _) => RefreshList();
        RefreshList();

        Closed += (_, _) => _closedTcs.TrySetResult();
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
            AddErrorText.Text = LocalizationService.Get("HistoryDialog_TitleRequired");
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
        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add(LocalizationService.Get("HistoryDialog_MarkdownType"), [".md"]);
        picker.SuggestedFileName = $"{Vm.ProjectName}_history";

        // FileSavePicker hwnd는 이 다이얼로그 창 자체를 사용
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is not null)
            await FileIO.WriteTextAsync(file, Vm.ExportToMarkdown());
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
