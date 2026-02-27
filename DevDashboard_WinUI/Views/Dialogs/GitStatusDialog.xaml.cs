using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Views.Dialogs;

public sealed partial class GitStatusDialog : ContentDialog
{
    private readonly ProjectCardViewModel _card;

    public GitStatusDialog(ProjectCardViewModel card)
    {
        _card = card;
        InitializeComponent();
        Title = $"Git 상태 — {card.Name}";
        Opened += OnOpened;
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        var error = await _card.LoadGitStatusAsync();

        LoadingPanel.Visibility = Visibility.Collapsed;

        if (error is not null)
        {
            ErrorBar.Message = error;
            ErrorBar.IsOpen = true;
            return;
        }

        BranchText.Text = _card.GitBranch;
        CommitList.ItemsSource = _card.GitChangedFiles;

        NoCommitsText.Visibility = _card.GitChangedFiles.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ContentPanel.Visibility = Visibility.Visible;
    }
}
