using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevDashboard.Views.Dialogs;

public sealed partial class HistoryDialog : ContentDialog
{
    private HistoryDialogViewModel Vm { get; }

    public HistoryDialog(HistoryDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
        Title = string.Format(LocalizationService.Get("HistoryDialog_TitleFormat"), vm.ProjectName);
        Vm.PropertyChanged += (_, _) => RefreshList();
        RefreshList();
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

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is not null)
            await FileIO.WriteTextAsync(file, Vm.ExportToMarkdown());
    }
}
