using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

        GroupList.ItemsSource = Vm.DateGroups;
        Vm.PropertyChanged += (_, _) => RefreshList();
        RefreshList();

        // 창 닫힐 때 SaveAll 호출 후 Task 완료
        // SaveAll 실패 시에도 _closedTcs.TrySetResult()가 반드시 호출되도록 finally 보장
        Closed += (_, _) =>
        {
            try
            {
                Vm.SaveAll();
            }
            catch (Exception ex)
            {
                _ = DialogService.ShowErrorAsync(ex.Message, LocalizationService.Get("SaveError"));
            }
            finally
            {
                _closedTcs.TrySetResult();
            }
        };
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    // ObservableCollection이 CollectionChanged로 자동 갱신하므로 ItemsSource 재할당 불필요
    private void RefreshList()
    {
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
            AddDatePicker.Date = DateTimeOffset.Now;
        }
    }

    private async void EditEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: HistoryEntryViewModel entryVm }) return;

        var titleBox = new TextBox { Text = entryVm.Model.Title, MaxLength = 200 };
        var descBox = new TextBox
        {
            Text = entryVm.Model.Description,
            AcceptsReturn = true,
            MinHeight = 70,
            TextWrapping = TextWrapping.Wrap
        };
        ScrollViewer.SetVerticalScrollBarVisibility(descBox, ScrollBarVisibility.Auto);

        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(titleBox);
        panel.Children.Add(descBox);

        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("HistoryEditHeader"),
            Content = panel,
            PrimaryButtonText = LocalizationService.Get("Btn_Save.Content"),
            CloseButtonText = LocalizationService.Get("Btn_Cancel.Content"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var title = titleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title)) return;

        Vm.UpdateEntry(entryVm, title, descBox.Text.Trim(), entryVm.Model.CompletedAt);
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
        var date = AddDatePicker.Date?.Date ?? DateTime.Today;
        var desc = AddDescriptionBox.Text.Trim();

        Vm.AddEntry(new HistoryEntry { Title = title, Description = desc, CompletedAt = date });
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

    // 항목 Border 클릭 → 펼침/접힘 토글
    private void EntryBorder_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border { Tag: HistoryEntryViewModel entryVm })
            entryVm.ToggleExpandCommand.Execute(null);
    }

    // 수정 버튼 Tapped → 부모 Border의 Tapped 이벤트 전파 차단
    private void EditBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    // 삭제 버튼 Tapped → 부모 Border의 Tapped 이벤트 전파 차단
    private void DeleteBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
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
