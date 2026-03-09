using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class GitStatusDialog : ContentDialog
{
    private readonly ProjectCardViewModel _card;
    private readonly CancellationTokenSource _cts = new();

    public GitStatusDialog(ProjectCardViewModel card)
    {
        _card = card;
        InitializeComponent();

        // 프로젝트명을 포함한 동적 타이틀 설정
        Title = string.Format(LocalizationService.Get("GitStatusDialog_TitleFormat"), card.Name);
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        Closing += (_, _) =>
        {
            _cts.Cancel();
            _cts.Dispose();
        };
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        try
        {
            string? error;
            try
            {
                error = await _card.LoadGitStatusAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            LoadingPanel.Visibility = Visibility.Collapsed;

            if (error is not null)
            {
                ErrorBar.Message = error;
                ErrorBar.IsOpen = true;
                return;
            }

            BranchText.Text = _card.GitBranch;
            CommitList.ItemsSource = _card.GitCommitGroups;

            NoCommitsText.Visibility = _card.GitCommitGroups.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            ContentPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorBar.Message = string.Format(LocalizationService.Get("UnexpectedError"), ex.Message);
            ErrorBar.IsOpen = true;
        }
    }
}
