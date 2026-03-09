using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class ProjectHistoryDialog : ContentDialog
{
    private ProjectHistoryDialogViewModel Vm { get; }
    private TaskCompletionSource<bool>? _nestedTcs;
    private readonly System.ComponentModel.PropertyChangedEventHandler _vmPropertyChangedHandler;

    public ProjectHistoryDialog(IReadOnlyList<ProjectItem> projects, IProjectRepository repository)
    {
        Vm = new ProjectHistoryDialogViewModel(projects.ToList(), repository);
        InitializeComponent();

        Title = LocalizationService.Get("ProjectHistoryDialogTitle");
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        GroupList.ItemsSource = Vm.DateGroups;
        _vmPropertyChangedHandler = (_, _) => RefreshList();
        Vm.PropertyChanged += _vmPropertyChangedHandler;
        RefreshList();

        Closing += (_, _) =>
        {
            Vm.PropertyChanged -= _vmPropertyChangedHandler;
            try
            {
                Vm.SaveAll();
            }
            catch
            {
                // Closing 이벤트에서는 중첩 ContentDialog 표시 불가 — 에러 무시
            }
        };
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        ContentDialogResult result;
        do
        {
            result = await base.ShowAsync();
            if (_nestedTcs is not null)
            {
                await _nestedTcs.Task;
                _nestedTcs = null;
                continue;
            }
            break;
        } while (true);
        return result;
    }

    private async Task<ContentDialogResult> ShowNestedDialogAsync(ContentDialog dialog)
    {
        _nestedTcs = new TaskCompletionSource<bool>();
        Hide();
        dialog.XamlRoot = App.MainWindow?.Content?.XamlRoot;
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _nestedTcs.TrySetResult(true);
        }
    }

    private Task ShowNestedErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("Dialog_DefaultErrorTitle"),
            Content = string.Format(LocalizationService.Get("UnexpectedError"), message),
            CloseButtonText = LocalizationService.Get("Dialog_OK"),
        };
        return ShowNestedDialogAsync(dialog);
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
        try
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
                PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            var title = titleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title)) return;

            Vm.UpdateEntry(entryVm, title, descBox.Text.Trim(), entryVm.Model.CompletedAt);
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
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
        try
        {
            if (Vm.SelectedProject is null) return;

            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add(LocalizationService.Get("HistoryDialog_MarkdownType"), [".md"]);
            picker.SuggestedFileName = $"{Vm.SelectedProject.Name}_history";

            // FileSavePicker hwnd는 MainWindow를 사용
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is not null)
                await FileIO.WriteTextAsync(file, Vm.ExportToMarkdown());
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }
}
